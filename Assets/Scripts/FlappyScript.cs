using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

/// <summary>
/// </summary>
public class FlappyScript : MonoBehaviour
{

    public AudioClip FlyAudioClip, DeathAudioClip, ScoredAudioClip;
    public Sprite GetReadySprite;
    public float RotateUpSpeed = 1, RotateDownSpeed = 1;
    public GameObject IntroGUI, DeathGUI;
    public Collider2D restartButtonGameCollider;
    public float VelocityPerJump = 3;
    public float XSpeed = 1;

    public float sensitivity = 100;
    public float loudness = 0;
    public AudioSource audioSource;
    public bool usingVoice;
    public float minVolume = 2f;
    public bool jumpFlag;
    public AudioMixerGroup audioMixerGroup;
    // Use this for initialization
    void Start()
    {
        audioSource.outputAudioMixerGroup=audioMixerGroup;
        //GetComponent<AudioSource>().clip = Microphone.Start(null, true, 10, 44100);
        audioSource.clip = Microphone.Start(null, true, 10, 44100);
        //GetComponent<AudioSource>().loop = true; // Set the AudioClip to loop
        audioSource.loop = true;
        //GetComponent<AudioSource>().mute = true; // Mute the sound, we don't want the player to hear it
        while (!(Microphone.GetPosition("") > 0))
        {
        } // Wait until the recording has started
        audioSource.Play(); // Play the audio source!
    }

    FlappyYAxisTravelState flappyYAxisTravelState;

    enum FlappyYAxisTravelState
    {
        GoingUp, GoingDown
    }

    Vector3 birdRotation = Vector3.zero;
    // Update is called once per frame
    void Update()
    {
        loudness = GetAveragedVolume() * sensitivity;

        //handle back key in Windows Phone
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();

        if (GameStateManager.GameState == GameState.Intro)
        {
            MoveBirdOnXAxis();
            if (usingVoice) 
            {
                if (loudness >= minVolume) {
                    BoostOnYAxis();
                    GameStateManager.GameState = GameState.Playing;
                    IntroGUI.SetActive(false);
                    ScoreManagerScript.Score = 0;
                    this.jumpFlag=true;
                }
            } else {

                if (WasTouchedOrClicked())
                {
                    BoostOnYAxis();
                    GameStateManager.GameState = GameState.Playing;
                    IntroGUI.SetActive(false);
                    ScoreManagerScript.Score = 0;
                }
            }
            
        }

        else if (GameStateManager.GameState == GameState.Playing)
        {           
            if (jumpFlag){
                if (loudness < minVolume ) {
                    jumpFlag = false;
                }
            }
            MoveBirdOnXAxis();
            if (usingVoice) 
            {
                if (loudness >= minVolume && !jumpFlag) {
                    BoostOnYAxis();
                    jumpFlag=true;
                }
            } else { 

                if (WasTouchedOrClicked())
                {
                    BoostOnYAxis();
                }
            }
            

        }

        else if (GameStateManager.GameState == GameState.Dead)
        {
            if (usingVoice) {
                if (loudness <= minVolume) {
                    GameStateManager.GameState = GameState.Intro;
                    Application.LoadLevel(Application.loadedLevelName);
                }

            } else {
                    Vector2 contactPoint = Vector2.zero;

                if (Input.touchCount > 0)
                    contactPoint = Input.touches[0].position;
                if (Input.GetMouseButtonDown(0))
                    contactPoint = Input.mousePosition;

                //check if user wants to restart the game
                if (restartButtonGameCollider == Physics2D.OverlapPoint
                    (Camera.main.ScreenToWorldPoint(contactPoint)))
                {
                    GameStateManager.GameState = GameState.Intro;
                    Application.LoadLevel(Application.loadedLevelName);
                }
            }
            
        }

    }


    void FixedUpdate()
    {
        //just jump up and down on intro screen
        if (GameStateManager.GameState == GameState.Intro)
        {
            if (GetComponent<Rigidbody2D>().velocity.y < -1) //when the speed drops, give a boost
                GetComponent<Rigidbody2D>().AddForce(new Vector2(0, GetComponent<Rigidbody2D>().mass * 5500 * Time.deltaTime)); //lots of play and stop 
                                                        //and play and stop etc to find this value, feel free to modify
        }
        else if (GameStateManager.GameState == GameState.Playing || GameStateManager.GameState == GameState.Dead)
        {
            FixFlappyRotation();
        }
    }

    bool WasTouchedOrClicked()
    {
        if (Input.GetButtonUp("Jump") || Input.GetMouseButtonDown(0) || 
            (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Ended))
            return true;
        else
            return false;
    }

    float GetAveragedVolume()
    {
        float[] data = new float[256];
        float a = 0;
        //GetComponent<AudioSource>().GetOutputData(data, 0);
        audioSource.GetOutputData(data, 0);
        foreach (float s in data)
        {
            a += Mathf.Abs(s);
            //Debug.Log("a " + a);
            //Debug.Log("/a " + a/256);
        }
        return a / 256;
    }

    void MoveBirdOnXAxis()
    {
        transform.position += new Vector3(Time.deltaTime * XSpeed, 0, 0);
    }

    void BoostOnYAxis()
    {
        GetComponent<Rigidbody2D>().velocity = new Vector2(0, VelocityPerJump);
        GetComponent<AudioSource>().PlayOneShot(FlyAudioClip);
    }



    /// <summary>
    /// when the flappy goes up, it'll rotate up to 45 degrees. when it falls, rotation will be -90 degrees min
    /// </summary>
    private void FixFlappyRotation()
    {
        if (GetComponent<Rigidbody2D>().velocity.y > 0) flappyYAxisTravelState = FlappyYAxisTravelState.GoingUp;
        else flappyYAxisTravelState = FlappyYAxisTravelState.GoingDown;

        float degreesToAdd = 0;

        switch (flappyYAxisTravelState)
        {
            case FlappyYAxisTravelState.GoingUp:
                degreesToAdd = 6 * RotateUpSpeed;
                break;
            case FlappyYAxisTravelState.GoingDown:
                degreesToAdd = -3 * RotateDownSpeed;
                break;
            default:
                break;
        }
        //solution with negative eulerAngles found here: http://answers.unity3d.com/questions/445191/negative-eular-angles.html

        //clamp the values so that -90<rotation<45 *always*
        birdRotation = new Vector3(0, 0, Mathf.Clamp(birdRotation.z + degreesToAdd, -90, 45));
        transform.eulerAngles = birdRotation;
    }

    /// <summary>
    /// check for collision with pipes
    /// </summary>
    /// <param name="col"></param>
    void OnTriggerEnter2D(Collider2D col)
    {
        if (GameStateManager.GameState == GameState.Playing)
        {
            if (col.gameObject.tag == "Pipeblank") //pipeblank is an empty gameobject with a collider between the two pipes
            {
                GetComponent<AudioSource>().PlayOneShot(ScoredAudioClip);
                ScoreManagerScript.Score++;
            }
            else if (col.gameObject.tag == "Pipe")
            {
                FlappyDies();
            }
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (GameStateManager.GameState == GameState.Playing)
        {
            if (col.gameObject.tag == "Floor")
            {
                FlappyDies();
            }
        }
    }

    void FlappyDies()
    {
        GameStateManager.GameState = GameState.Dead;
        DeathGUI.SetActive(true);
        GetComponent<AudioSource>().PlayOneShot(DeathAudioClip);
    }

}
