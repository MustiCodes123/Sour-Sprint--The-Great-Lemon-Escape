using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

/// <summary>
/// Handle everything related to controlling the character. Interact with both the Character (visual, animation) and CharacterCollider
/// </summary>
public class CharacterInputController : MonoBehaviour
{
    static int s_DeadHash = Animator.StringToHash ("Dead");
	static int s_RunStartHash = Animator.StringToHash("runStart");
	static int s_MovingHash = Animator.StringToHash("Moving");
	static int s_JumpingHash = Animator.StringToHash("Jumping");
	static int s_JumpingSpeedHash = Animator.StringToHash("JumpSpeed");
	static int s_SlidingHash = Animator.StringToHash("Sliding");

	public TrackManager trackManager;
	public Character character;
	public CharacterCollider characterCollider;
	public GameObject blobShadow;
	public float laneChangeSpeed = 1.0f;

	public int maxLife = 3;

	public Consumable inventory;

	public int coins { get { return m_Coins; } set { m_Coins = value; } }
	public int premium { get { return m_Premium; } set { m_Premium = value; } }
	public int currentLife { get { return m_CurrentLife; } set { m_CurrentLife = value; } }
	public List<Consumable> consumables { get { return m_ActiveConsumables; } }
	public bool isJumping { get { return m_Jumping; } }
	public bool isSliding { get { return m_Sliding; } }

	[Header("Controls")]
	public float jumpLength = 2.0f;     // Distance jumped
	public float jumpHeight = 1.2f;

	public float slideLength = 2.0f;

	[Header("Sounds")]
	public AudioClip slideSound;
	public AudioClip powerUpUseSound;
	public AudioSource powerupSource;

    [HideInInspector] public int currentTutorialLevel;
    [HideInInspector] public bool tutorialWaitingForValidation;

    protected int m_Coins;
    protected int m_Premium;
    protected int m_CurrentLife;

    protected List<Consumable> m_ActiveConsumables = new List<Consumable>();

    protected int m_ObstacleLayer;

	protected bool m_IsInvincible;
	protected bool m_IsRunning;
	
    protected float m_JumpStart;
    protected bool m_Jumping;

	protected bool m_Sliding;
	protected float m_SlideStart;

	protected AudioSource m_Audio;

    protected int m_CurrentLane = k_StartingLane;
    protected Vector3 m_TargetPosition = Vector3.zero;

    protected readonly Vector3 k_StartingPosition = Vector3.forward * 2f;

    protected const int k_StartingLane = 1;
    protected const float k_GroundingSpeed = 80f;
    protected const float k_ShadowRaycastDistance = 100f;
    protected const float k_ShadowGroundOffset = 0.01f;
    protected const float k_TrackSpeedToJumpAnimSpeedRatio = 0.6f;
    protected const float k_TrackSpeedToSlideAnimSpeedRatio = 0.9f;

    protected void Awake ()
    {
        m_Premium = 0;
        m_CurrentLife = 0;
        m_Sliding = false;
        m_SlideStart = 0.0f;
	    m_IsRunning = false;
    }

#if !UNITY_STANDALONE
    protected Vector2 m_StartingTouch;
	protected bool m_IsSwiping = false;
#endif

    // Cheating functions, use for testing
	public void CheatInvincible(bool invincible)
	{
		m_IsInvincible = invincible;
    }

	public bool IsCheatInvincible()
	{
		return m_IsInvincible;
	}

    public void Init()
    {
        transform.position = k_StartingPosition;
		m_TargetPosition = Vector3.zero;

		m_CurrentLane = k_StartingLane;
		characterCollider.transform.localPosition = Vector3.zero;

        currentLife = maxLife;

		m_Audio = GetComponent<AudioSource>();

		m_ObstacleLayer = 1 << LayerMask.NameToLayer("Obstacle");
    }

	// Called at the beginning of a run or rerun
	public void Begin()
	{
		m_IsRunning = false;
        character.animator.SetBool(s_DeadHash, false);

		characterCollider.Init ();

		m_ActiveConsumables.Clear();
	}

	public void End()
	{
        CleanConsumable();
    }

    public void CleanConsumable()
    {
        for (int i = 0; i < m_ActiveConsumables.Count; ++i)
        {
            m_ActiveConsumables[i].Ended(this);
            Addressables.ReleaseInstance(m_ActiveConsumables[i].gameObject);
        }

        m_ActiveConsumables.Clear();
    }

    public void StartRunning()
    {   
	    StartMoving();
        if (character.animator)
        {
            character.animator.Play(s_RunStartHash);
            character.animator.SetBool(s_MovingHash, true);
        }
    }

	public void StartMoving()
	{
		m_IsRunning = true;
	}

    public void StopMoving()
    {
	    m_IsRunning = false;
        trackManager.StopMove();
        if (character.animator)
        {
            character.animator.SetBool(s_MovingHash, false);
        }
    }

    protected bool TutorialMoveCheck(int tutorialLevel)
    {
        tutorialWaitingForValidation = currentTutorialLevel != tutorialLevel;

        return (!TrackManager.instance.isTutorial || currentTutorialLevel >= tutorialLevel);
    }

	protected void Update ()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        // Use key input in editor or standalone
        // disabled if it's tutorial and not thecurrent right tutorial level (see func TutorialMoveCheck)

        if (Input.GetKeyDown(KeyCode.LeftArrow) && TutorialMoveCheck(0))
        {
            ChangeLane(-1);
        }
        else if(Input.GetKeyDown(KeyCode.RightArrow) && TutorialMoveCheck(0))
        {
            ChangeLane(1);
        }
        else if(Input.GetKeyDown(KeyCode.UpArrow) && TutorialMoveCheck(1))
        {
            Jump();
        }
		else if (Input.GetKeyDown(KeyCode.DownArrow) && TutorialMoveCheck(2))
		{
			if(!m_Sliding)
				Slide();
		}
		
		// SIMULATOR SUPPORT: Allow mouse to simulate touch in editor for testing
		#if UNITY_EDITOR
		if(Input.GetMouseButtonDown(0))
		{
			m_StartingTouch = Input.mousePosition;
			m_IsSwiping = true;
			Debug.Log($"[MOUSE SIMULATOR] Mouse down at {m_StartingTouch}");
		}
		else if(Input.GetMouseButtonUp(0) && m_IsSwiping)
		{
			Vector2 diff = (Vector2)Input.mousePosition - m_StartingTouch;
			Vector2 diffNormalized = new Vector2(diff.x/Screen.width, diff.y/Screen.width);
			
			Debug.Log($"[MOUSE SIMULATOR] Mouse up - Diff: {diff}, Normalized: {diffNormalized}, Magnitude: {diffNormalized.magnitude}");
			
			// Check if this is a swipe or tap
			if(diffNormalized.magnitude > 0.02f) // Swipe
			{
				if(Mathf.Abs(diffNormalized.y) > Mathf.Abs(diffNormalized.x))
				{
					// Vertical swipe
					if(TutorialMoveCheck(2) && diffNormalized.y < 0)
					{
						Debug.Log("[MOUSE SIMULATOR] SWIPE DOWN - Sliding");
						Slide();
					}
				}
				else if(TutorialMoveCheck(0))
				{
					// Horizontal swipe
					if(diffNormalized.x < 0)
					{
						Debug.Log("[MOUSE SIMULATOR] SWIPE LEFT");
						ChangeLane(-1);
					}
					else
					{
						Debug.Log("[MOUSE SIMULATOR] SWIPE RIGHT");
						ChangeLane(1);
					}
				}
			}
			else if(TutorialMoveCheck(1)) // Tap
			{
				Debug.Log("[MOUSE SIMULATOR] TAP - Jumping");
				Jump();
			}
			
			m_IsSwiping = false;
		}
		#endif
#else
        // Use touch input on mobile - Enhanced for tap and swipe with debugging
        if (Input.touchCount == 1)
        {
			Touch touch = Input.GetTouch(0);
			
			// DEBUG: Log touch phase
			if(touch.phase == TouchPhase.Began)
			{
				Debug.Log($"[TOUCH] Touch Began at {touch.position}");
			}
			
			if(m_IsSwiping)
			{
				Vector2 diff = touch.position - m_StartingTouch;
				Vector2 diffNormalized = new Vector2(diff.x/Screen.width, diff.y/Screen.width);

				// DEBUG: Log swipe progress during movement
				if(touch.phase == TouchPhase.Moved)
				{
					Debug.Log($"[TOUCH] Swiping - Diff: {diff}, Normalized: {diffNormalized}, Magnitude: {diffNormalized.magnitude}");
				}

				// Reduced threshold for better responsiveness - 0.5% of screen width
				if(diffNormalized.magnitude > 0.005f)
				{
					// Check if this is a swipe (not a tap)
					if(diffNormalized.magnitude > 0.02f) // Swipe threshold - 2% of screen width
					{
						if(Mathf.Abs(diffNormalized.y) > Mathf.Abs(diffNormalized.x))
						{
							// Vertical swipe
							if(TutorialMoveCheck(2) && diffNormalized.y < 0)
							{
								// Swipe down - Slide/Ground Slam
								Debug.Log($"[TOUCH] SWIPE DOWN detected! Magnitude: {diffNormalized.magnitude}");
								Slide();
							}
							else if(diffNormalized.y > 0)
							{
								Debug.Log($"[TOUCH] Swipe UP detected (ignored) Magnitude: {diffNormalized.magnitude}");
							}
						}
						else if(TutorialMoveCheck(0))
						{
							// Horizontal swipe - Lane change
							if(diffNormalized.x < 0)
							{
								Debug.Log($"[TOUCH] SWIPE LEFT detected! Magnitude: {diffNormalized.magnitude}");
								ChangeLane(-1);
							}
							else
							{
								Debug.Log($"[TOUCH] SWIPE RIGHT detected! Magnitude: {diffNormalized.magnitude}");
								ChangeLane(1);
							}
						}
						
						m_IsSwiping = false;
					}
				}
            }

        	// Input check is AFTER the swipe test
			if(touch.phase == TouchPhase.Began)
			{
				m_StartingTouch = touch.position;
				m_IsSwiping = true;
				Debug.Log($"[TOUCH] Started tracking swipe from {m_StartingTouch}");
			}
			else if(touch.phase == TouchPhase.Ended)
			{
				// Check if this was a tap (small movement) instead of a swipe
				if(m_IsSwiping)
				{
					Vector2 diff = touch.position - m_StartingTouch;
					Vector2 diffNormalized = new Vector2(diff.x/Screen.width, diff.y/Screen.width);
					
					Debug.Log($"[TOUCH] Touch Ended - Diff: {diff}, Normalized Magnitude: {diffNormalized.magnitude}");
					
					// If movement was minimal, treat as tap for jump
					if(diffNormalized.magnitude < 0.02f && TutorialMoveCheck(1))
					{
						Debug.Log($"[TOUCH] TAP detected! Jumping...");
						Jump();
					}
					else
					{
						Debug.Log($"[TOUCH] Movement too large for tap ({diffNormalized.magnitude}), treated as swipe");
					}
				}
				
				m_IsSwiping = false;
			}
        }
		else if(Input.touchCount == 0)
		{
			// Reset swipe state if no touches
			m_IsSwiping = false;
		}
		else if(Input.touchCount > 1)
		{
			Debug.Log($"[TOUCH] Multiple touches detected ({Input.touchCount}), ignoring");
		}
#endif

        Vector3 verticalTargetPosition = m_TargetPosition;

		if (m_Sliding)
		{
            // Slide time isn't constant but the slide length is (even if slightly modified by speed, to slide slightly further when faster).
            // This is for gameplay reason, we don't want the character to drasticly slide farther when at max speed.
			float correctSlideLength = slideLength * (1.0f + trackManager.speedRatio);
			float ratio = (trackManager.worldDistance - m_SlideStart) / correctSlideLength;
			if (ratio >= 1.0f)
			{
                // We slid to (or past) the required length, go back to running
				StopSliding();
			}
		}

        if(m_Jumping)
        {
			if (trackManager.isMoving)
			{
                // Same as with the sliding, we want a fixed jump LENGTH not fixed jump TIME. Also, just as with sliding,
                // we slightly modify length with speed to make it more playable.
				float correctJumpLength = jumpLength * (1.0f + trackManager.speedRatio);
				float ratio = (trackManager.worldDistance - m_JumpStart) / correctJumpLength;
				if (ratio >= 1.0f)
				{
					m_Jumping = false;
					character.animator.SetBool(s_JumpingHash, false);
				}
				else
				{
					verticalTargetPosition.y = Mathf.Sin(ratio * Mathf.PI) * jumpHeight;
				}
			}
			else if(!AudioListener.pause)//use AudioListener.pause as it is an easily accessible singleton & it is set when the app is in pause too
			{
			    verticalTargetPosition.y = Mathf.MoveTowards (verticalTargetPosition.y, 0, k_GroundingSpeed * Time.deltaTime);
				if (Mathf.Approximately(verticalTargetPosition.y, 0f))
				{
					character.animator.SetBool(s_JumpingHash, false);
					m_Jumping = false;
				}
			}
        }

        characterCollider.transform.localPosition = Vector3.MoveTowards(characterCollider.transform.localPosition, verticalTargetPosition, laneChangeSpeed * Time.deltaTime);

        // Put blob shadow under the character.
        RaycastHit hit;
        if(Physics.Raycast(characterCollider.transform.position + Vector3.up, Vector3.down, out hit, k_ShadowRaycastDistance, m_ObstacleLayer))
        {
            blobShadow.transform.position = hit.point + Vector3.up * k_ShadowGroundOffset;
        }
        else
        {
            Vector3 shadowPosition = characterCollider.transform.position;
            shadowPosition.y = k_ShadowGroundOffset;
            blobShadow.transform.position = shadowPosition;
        }
	}

    public void Jump()
    {
	    if (!m_IsRunning)
		    return;
	    
        if (!m_Jumping)
        {
			if (m_Sliding)
				StopSliding();

			float correctJumpLength = jumpLength * (1.0f + trackManager.speedRatio);
			m_JumpStart = trackManager.worldDistance;
            float animSpeed = k_TrackSpeedToJumpAnimSpeedRatio * (trackManager.speed / correctJumpLength);

            character.animator.SetFloat(s_JumpingSpeedHash, animSpeed);
            character.animator.SetBool(s_JumpingHash, true);
			m_Audio.PlayOneShot(character.jumpSound);
			m_Jumping = true;
        }
    }

    public void StopJumping()
    {
        if (m_Jumping)
        {
            character.animator.SetBool(s_JumpingHash, false);
            m_Jumping = false;
        }
    }

	public void Slide()
	{
		if (!m_IsRunning)
			return;
		
		if (!m_Sliding)
		{

		    if (m_Jumping)
		        StopJumping();

            float correctSlideLength = slideLength * (1.0f + trackManager.speedRatio); 
			m_SlideStart = trackManager.worldDistance;
            float animSpeed = k_TrackSpeedToJumpAnimSpeedRatio * (trackManager.speed / correctSlideLength);

			character.animator.SetFloat(s_JumpingSpeedHash, animSpeed);
			character.animator.SetBool(s_SlidingHash, true);
			m_Audio.PlayOneShot(slideSound);
			m_Sliding = true;

			characterCollider.Slide(true);
		}
	}

	public void StopSliding()
	{
		if (m_Sliding)
		{
			character.animator.SetBool(s_SlidingHash, false);
			m_Sliding = false;

			characterCollider.Slide(false);
		}
	}

	public void ChangeLane(int direction)
    {
		if (!m_IsRunning)
			return;

        int targetLane = m_CurrentLane + direction;

        if (targetLane < 0 || targetLane > 2)
            // Ignore, we are on the borders.
            return;

        m_CurrentLane = targetLane;
        m_TargetPosition = new Vector3((m_CurrentLane - 1) * trackManager.laneOffset, 0, 0);
    }

    public void UseInventory()
    {
        if(inventory != null && inventory.CanBeUsed(this))
        {
            UseConsumable(inventory);
            inventory = null;
        }
    }

    public void UseConsumable(Consumable c)
    {
		characterCollider.audio.PlayOneShot(powerUpUseSound);

        for(int i = 0; i < m_ActiveConsumables.Count; ++i)
        {
            if(m_ActiveConsumables[i].GetType() == c.GetType())
            {
				// If we already have an active consumable of that type, we just reset the time
                m_ActiveConsumables[i].ResetTime();
                Addressables.ReleaseInstance(c.gameObject);
                return;
            }
        }

        // If we didn't had one, activate that one 
        c.transform.SetParent(transform, false);
        c.gameObject.SetActive(false);

        m_ActiveConsumables.Add(c);
        StartCoroutine(c.Started(this));
    }
}
