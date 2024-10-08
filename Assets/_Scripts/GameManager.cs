using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NamruSessionManagementSystem;
using UnityEditor;
using LogansAlarmSystem;
using UnityEngine.Events;

namespace OldWay
{
    public class GameManager : NSMS_Object
    {
        public static GameManager Instance;

        public static GameState CurrentGameState = GameState.WaitingToBegin;

        [Header("[---- REFERENCE ----]")]
        public ImvestController CurrentController;
        public GameObject[] RingObjects;
        private Animator[] RingAnimators;
        private ImvestRing[] RingScripts;

        [Space(10)]
        [SerializeField] private Rigidbody rb_player;
        [SerializeField] private InGameCanvas _inGameCanvasScript;
        public PromptCollection _PromptCollection;
        public ImvestPrompt CurrentPrompt
        {
            get
            {
                return _PromptCollection.prompts[index_currentPrompt];
            }
        }

        /// <summary>
        /// Keeps track of which ring is next to explode upon incorrect choice.
        /// </summary>
        private int Index_currentRing = 0;

        [Header("[---- AUDIO ----]")]
        [SerializeField] private AudioClip explosionClip;

        [Header("[---- STATS ----]")]
        public int amount_pointsPerPrompt = 100;

        [Tooltip("Length, in seconds, of an entire session.")]
        public float duration_entireSession = 60f;

        [Header("[---- ALARMS ----]")]
        public Alarm Alarm_cd_countingDownBeforeFirstPrompt;
        public Alarm Alarm_cd_answeringPrompt;
        public Alarm Alarm_cd_ringIsExploding;
        public Alarm Alarm_cd_feedbackIsBeingDisplayed;

        //[Header("[---- OTHER ----]")]
        /// <summary>
        /// This is a flag for indicating whether the participant has actually pressed a button to make a choice. Gets set to false at beginning of each prompt.
        /// </summary>
        [HideInInspector] public bool flag_haveMadeSelection = false;
        [HideInInspector] public bool flag_lastSelectionWasCorrect = false;

        [Header("[---- RESULTS ----]")]
        [HideInInspector] public int runningScore = 0;
        /// <summary>
        /// Keeps track of running amount of questions that are correct. Gets updated with every answer.
        /// </summary>
        [HideInInspector] public int numberCorrect = 0;
        /// <summary>
        /// Keeps track of how many questions have been presented to the participant.
        /// </summary>
        [HideInInspector] public int runningNumberOfQuestionsPresented = 0;
        /// <summary>
        /// keeps track of running correct answer percentage. Gets updated with every answer.
        /// </summary>
        [HideInInspector] public float correctAnswerPercentage = 0f;

        //[Header("[---- OTHER ----]")]
        [HideInInspector] public int index_currentPrompt = 0;
        /// <summary>
        /// Keeps track of the running time of this entire session. The session/scenario/program will end when this value reaches duration_entireSession
        /// </summary>
        [HideInInspector] public float RunningTime_entireSession = 0f;



        [Header("DEBUG")]
        public string DBGstring;

        [Space(10f)]
        [SerializeField] private string DBG_stateHistory;


        private void Awake()
        {
            LogInc("Awake()");

            Instance = this;

            RingScripts = new ImvestRing[RingObjects.Length];
            RingAnimators = new Animator[RingObjects.Length];
            for (int i = 0; i < RingObjects.Length; i++)
            {
                RingAnimators[i] = RingObjects[i].GetComponent<Animator>();
                RingScripts[i] = RingObjects[i].GetComponent<ImvestRing>();
            }

            NamruLogManager.DecrementTabLevel();
        }

        void Start()
        {
            LogInc("Start()");

            Index_currentRing = 0;
            index_currentPrompt = 0;

            rb_player.useGravity = false;

            Alarm_cd_countingDownBeforeFirstPrompt.Reset();
            Alarm_cd_answeringPrompt.Reset();
            Alarm_cd_ringIsExploding.Reset();
            Alarm_cd_feedbackIsBeingDisplayed.Reset();

            CheckIfKosher();

            NamruLogManager.DecrementTabLevel();
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Escape))
            {
                QuitApplication();
            }

            if (CurrentGameState != GameState.WaitingToBegin &&
                CurrentGameState != GameState.CountdownToStart &&
                CurrentGameState != GameState.Falling &&
                CurrentGameState != GameState.Finished)
            {
                RunningTime_entireSession += Time.deltaTime;

                if (RunningTime_entireSession >= duration_entireSession)
                {
                    ChangeState(GameState.Finished);
                }

            }


            if (CurrentGameState == GameState.CountdownToStart)
            {
                if (Alarm_cd_countingDownBeforeFirstPrompt.MoveTowardGoal(Time.deltaTime))
                {
                    ChangeState(GameState.SolvingPrompt);
                }
            }
            else if (CurrentGameState == GameState.SolvingPrompt)
            {
                if (Alarm_cd_answeringPrompt.MoveTowardGoal(Time.deltaTime))
                {
                    _inGameCanvasScript.ChoiceMade_action(false); //got to end of count with no choice made...
                }
            }
            else if (CurrentGameState == GameState.PromptSolvedFeedback)
            {
                if (Alarm_cd_feedbackIsBeingDisplayed.MoveTowardGoal(Time.deltaTime))
                {
                    //////////////////////////////////////////////
                    if (!flag_lastSelectionWasCorrect)
                    {
                        ChangeState(GameState.RingIsExploding);
                    }
                    else
                    {
                        ChangeState(GameState.SolvingPrompt);
                    }
                }
            }
            else if (CurrentGameState == GameState.RingIsExploding)
            {
                if (Alarm_cd_ringIsExploding.MoveTowardGoal(Time.deltaTime))
                {
                    ////////////////////
                    if (Index_currentRing >= RingObjects.Length)
                    {
                        ChangeState(GameState.Falling);
                    }
                    else
                    {
                        ChangeState(GameState.SolvingPrompt);
                    }
                }
            }
            else if (CurrentGameState == GameState.Falling)
            {
                if (rb_player.position.y < -6.8f)
                {
                    rb_player.useGravity = false;
                    rb_player.velocity = Vector3.zero;
                    rb_player.position = Vector3.zero;

                    foreach (ImvestRing ring in RingScripts)
                    {
                        ring.ResetMe();
                    }

                    Index_currentRing = 0;

                    ChangeState(GameState.SolvingPrompt);
                }
            }
            else if (CurrentGameState == GameState.Finished)
            {

            }

#if UNITY_EDITOR
            DBGstring = $"{nameof(CurrentGameState)}: '{CurrentGameState}'\n" +
                    $"{nameof(RunningTime_entireSession)}: '{RunningTime_entireSession}'\n\n" +

                    $"{nameof(Alarm_cd_answeringPrompt)}: '{Alarm_cd_answeringPrompt.CurrentValue.ToString("#.#")}' / {Alarm_cd_answeringPrompt.Duration}\n" +
                    $"{nameof(Alarm_cd_ringIsExploding)}: '{Alarm_cd_ringIsExploding.CurrentValue.ToString("#.#")}' / {Alarm_cd_ringIsExploding.Duration}\n" +
                    $"{nameof(Alarm_cd_feedbackIsBeingDisplayed)}: '{Alarm_cd_feedbackIsBeingDisplayed.CurrentValue.ToString("#.#")}' / {Alarm_cd_feedbackIsBeingDisplayed.Duration}\n" +

                    $"{nameof(index_currentPrompt)}: '{index_currentPrompt}'\n" +
                    $"{nameof(Index_currentRing)}: '{Index_currentRing}'\n\n" +

                    $"{nameof(numberCorrect)}: '{numberCorrect}'\n" +
                    $"{nameof(runningScore)}: '{runningScore}'\n" +
                    $"{nameof(correctAnswerPercentage)}: '{correctAnswerPercentage}'\n\n" +


                    $"";
#endif
        }

        public void ChangeState(GameState state)
        {
            LogInc($"{nameof(ChangeState)}({state})");

            GameState previousState = CurrentGameState;

            if (state == GameState.CountdownToStart)
            {

            }
            else if (state == GameState.SolvingPrompt)
            {
                RingAnimators[Index_currentRing].SetBool("b_pulse", true);

                if (previousState != GameState.CountdownToStart) //When this is called during the countdown, the index needs to stay on 0...
                {
                    index_currentPrompt++;

                    if (index_currentPrompt >= _PromptCollection.prompts.Length)
                    {
                        index_currentPrompt = 0;
                    }
                }

                flag_haveMadeSelection = false; //this is just a reset...
                flag_lastSelectionWasCorrect = false; //this is just a reset...

                Alarm_cd_answeringPrompt.Reset(); //todo: make the duration change dynamically

            }
            else if (state == GameState.PromptSolvedFeedback)
            {
                Alarm_cd_feedbackIsBeingDisplayed.Reset();
            }
            else if (state == GameState.RingIsExploding)
            {
                Alarm_cd_ringIsExploding.Reset();
                ExplodeRing();
            }
            else if (state == GameState.Falling)
            {
                rb_player.useGravity = true;
            }
            else if (state == GameState.Finished)
            {
                RingAnimators[Index_currentRing].SetBool("b_pulse", false);
            }

            CurrentGameState = state;
            DBG_stateHistory += $"{state}\n";

            _inGameCanvasScript.ChangeState(state, previousState);

            NamruLogManager.DecrementTabLevel();
        }

        public void InputTrigger()
        {
            if (CurrentGameState != GameState.SolvingPrompt)
            {
                return;
            }

            _inGameCanvasScript.InputTrigger();
        }

        public void InputLeft()
        {
            if ( CurrentGameState != GameState.SolvingPrompt )
            {
                return;
            }

            _inGameCanvasScript.InputLeft();
        }

        public void InputRight()
        {
            if ( CurrentGameState != GameState.SolvingPrompt )
            {
                return;
            }

            _inGameCanvasScript.InputRight();
        }

        public void ExplodeRing()
        {
            LogInc($"{nameof(ExplodeRing)}()");

            try
            {
                RingScripts[Index_currentRing].Explode();
                //RingObjects[Index_currentRing].SetActive( false );
                AudioSource.PlayClipAtPoint(explosionClip, rb_player.position, 0.5f);

                Index_currentRing++;
            }
            catch (System.Exception e)
            {
                NamruLogManager.LogException(e);
            }


            NamruLogManager.DecrementTabLevel();
        }

        public bool CheckIfKosher()
        {
            bool amKosher = true;

            if (RingObjects == null || RingObjects.Length <= 0)
            {
                amKosher = false;
                LogError($"{nameof(RingObjects)} reference was null or had 0 values!");
            }

            if (_inGameCanvasScript == null)
            {
                amKosher = false;
                LogError($"{nameof(_inGameCanvasScript)} reference was null!");
            }

            if (explosionClip == null)
            {
                amKosher = false;
                LogError($"{nameof(explosionClip)} reference was null!");
            }

            return amKosher;
        }


        [ContextMenu("z call TestLogs()")]
        public void TestLogs()
        {
            Log("this is console only", LogDestination.Console);
            Log("this is mdl only", LogDestination.MomentaryDebugLogger);
            LogWarning("this is a warning");
            LogError("this is an error");
        }


        [ContextMenu("z call SolvePrompts()")]
        public void SolvePrompts()
        {
            _PromptCollection.SolvePrompts();

#if UNITY_EDITOR
            Debug.Log("setting asset dirty...");
            UnityEditor.EditorUtility.SetDirty(_PromptCollection);
#endif
        }

        public void QuitApplication()
        {
            Log($"{nameof(QuitApplication)}()", LogDestination.Everywhere);

            if (NamruSessionManager.Instance != null)
            {
                NamruSessionManager.Instance.CloseMe();

            }

#if UNITY_EDITOR

            EditorApplication.isPlaying = false;
#else
		        Application.Quit();
#endif
        }
    }
}