using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NamruSessionManagementSystem;
using UnityEditor;
using LogansAlarmSystem;
using NAMRUScenarioSystem;
using Unity.VisualScripting;

namespace NewWay
{
    public class GameManager_new : NSMS_Object
    {
        public static GameManager_new Instance;

        [Header("[---- REFERENCE ----]")]
        private AudioSource _audioSource_mainCamera;
        public ImvestController_NSMS CurrentController;
        public GameObject[] RingObjects;
        private Animator[] RingAnimators;
        private ImvestRing[] RingScripts;

        [Space(10)]
        [SerializeField] private Rigidbody rb_player;
        private Collider playerCollider;
        [SerializeField] private InGameCanvas_new _inGameCanvasScript;
        public BinaryPromptCollection _PromptCollection;
        public BinaryPrompt CurrentPrompt
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
        [Tooltip("Length, in seconds, of an entire session.")]
        private float duration_entireSession = 120f;
        public static float AnswerPercentageThreshold_presentedAsAverage = 0.62f;

        [Header("[---- SCENARIO ----]")]
        public static int ScenarioIndex_WaitingToBegin = 0;
        public static int ScenarioIndex_CountdownToStart = 1;
        public static int ScenarioIndex_SolvingPrompt = 2;
        public static int ScenarioIndex_PromptAnsweredFeedback = 3;
        public static int ScenarioIndex_RingIsExploding = 4;
        public static int ScenarioIndex_Falling = 5;
        public static int ScenarioIndex_Berating = 6;
        public static int ScenarioIndex_Finished = 7;

        [Header("[---- RESULTS ----]")]
        /// <summary>
        /// Keeps track of running amount of questions that are correct. Gets updated with every answer.
        /// </summary>
        private int runningNumberOfPromptsCorrect = 0;
        /// <summary>
        /// Keeps track of how many prompts were presented to the participant as incorrect even though they were actually correct.
        /// </summary>
        private int runningNumberOfFalseNegatives = 0;
        /// <summary>
        /// Keeps track of how many questions have been presented to the participant.
        /// </summary>
        private int runningNumberOPromptsPresented = 0;
        /// <summary>
        /// keeps track of running correct answer percentage. Gets updated with every answer.
        /// </summary>
        private float correctAnswerPercentage = 0f;
        /// <summary>
        /// This is the percentage correct, considering the false negatives. IE: how the score is presented to the participant.
        /// </summary>
        [HideInInspector] public float CorrectAnswerPercentage_presented = 0f;

        [Header("[---- FLAGS ----]")]
        [HideInInspector] private bool flag_lastParticipantSelectionWasCorrect = false;
        /// <summary>
        /// This is a flag for indicating whether the participant has actually pressed a button to make a choice, as opposed to 
        /// letting the timer run out. Gets set to false at beginning of each prompt, and true only if participant selects one 
        /// of the choices..
        /// </summary>
        [HideInInspector] public bool Flag_participantHasMadeChoice = false;
        /// <summary>
        /// Flag that tells the system what feedback was given to the participant after any potential "false-negative". Note: 
        /// this does NOT necessarily represent whether the last prompt was indeed a false-negative, just definitively which 
        /// feedback was given to the participant. You can determine if the last evaluation was a false-negative by comparing 
        /// this value to the 'flag_lastParticipantSelectionWasCorrect' value.
        /// </summary>
        private bool flag_lastFeedbackGivenToParticpant = false;

        private bool flag_playerNeedsRespawn = false;
        /// <summary>
        /// Gets marked true during end of the prompt feedback stage so that at the end of the explosion stage, it can then know to go to the beratement stage
        /// </summary>
        private bool flag_needToGoToBeratementStage = false;

        [Header("[---- OTHER ----]")]
        [HideInInspector] public int index_currentPrompt = 0;

        private int index_currentBlock = 0;

        [Header("[---- BERATEMENT ----]")]
        [SerializeField] private AudioClip[] beratementClips;
        private float[] blockTimings;

        [Header("DEBUG")]
        public string DBGstring;

        [Space(10f)]
        [SerializeField] private string DBG_stateHistory;

        [ContextMenu("z call TryInvoke()")]
        public void TryInvoke()
        {
            ScenarioManager.Instance.Event_Warning.Invoke("hay");
        }

        private void Awake()
        {
            LogInc("Awake()");

            try
            {
                Instance = this;

                playerCollider = rb_player.GetComponent<Collider>();

                RingScripts = new ImvestRing[RingObjects.Length];
                RingAnimators = new Animator[RingObjects.Length];
                for ( int i = 0; i < RingObjects.Length; i++ )
                {
                    RingAnimators[i] = RingObjects[i].GetComponent<Animator>();
                    RingScripts[i] = RingObjects[i].GetComponent<ImvestRing>();
                }

                _audioSource_mainCamera = Camera.main.GetComponent<AudioSource>();
            }
            catch ( System.Exception e )
            {
                NamruLogManager.LogException( e );
            }


            NamruLogManager.DecrementTabLevel();
        }

        void Start()
        {
            LogInc("Start()");

            try
            {
                Index_currentRing = 0;
                index_currentPrompt = 0;

                rb_player.useGravity = false;
                ScenarioManager.Instance.Event_Warning.AddListener((string s) =>
                {
                    NamruLogManager.Log($"<color=red>{s}</color>", LogDestination.MomentaryDebugLogger);
                });

                #region STUFF ----------------
                blockTimings = new float[3] { 30f, 60f, 90f};
                index_currentBlock = 0;
                Log( $"{nameof(blockTimings)} length calculated as: '{blockTimings.Length}', " );
                #endregion
                CheckIfKosher();
            }
            catch ( System.Exception e )
            {
                NamruLogManager.LogException(e);
            }

            NamruLogManager.DecrementTabLevel();
        }

        private void OnStageAdvanceError_action( string s )
        {
            NamruLogManager.LogWarning(s);

        }

        [SerializeField] private bool flag_allowKeyboardInput = false;

        void Update()
        {
            if ( Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Escape) )
            {
                QuitApplication();
            }
            else if ( ScenarioManager.Instance.Index_currentOrderedStage == ScenarioIndex_SolvingPrompt )
            {
                if ( flag_allowKeyboardInput )
                {
                    if ( Input.GetKeyUp(KeyCode.A) )
                    {
                        InputLeft();
                    }
                    else if ( Input.GetKeyUp(KeyCode.D) )
                    {
                        InputRight();
                    }
                }
            }
            else if ( ScenarioManager.Instance.Index_currentOrderedStage == ScenarioIndex_Falling )
            {
                if ( flag_playerNeedsRespawn && rb_player.position.y < -15 )
                {
                    rb_player.position = new Vector3( 0f, 25f, 0f );

                    foreach ( ImvestRing ring in RingScripts )
                    {
                        ring.ResetMe();
                    }

                    flag_playerNeedsRespawn = false;
                }
                else if ( !flag_playerNeedsRespawn && rb_player.position.y <= 0.2f )
                {
                    ResetPlayerAndRingsAfterFall();
                }
            }

#if UNITY_EDITOR
                DBGstring = $"<b><u>SCENARIO MANAGER</u></b>\n" +
                $"{ScenarioManager.Instance.GetDiagnosticString()}\n\n" +

                 $"<b><u>GAME MANAGER</u></b>\n" +
                $"{nameof(index_currentPrompt)}: '{index_currentPrompt}'\n" +
                $"{nameof(index_currentBlock)}: '{index_currentBlock}'\n" +
                $"{nameof(Index_currentRing)}: '{Index_currentRing}'\n" +
                $"{nameof(Flag_participantHasMadeChoice)}: '{Flag_participantHasMadeChoice}'\n" +
                $"{nameof(flag_lastParticipantSelectionWasCorrect)}: '{flag_lastParticipantSelectionWasCorrect}'\n" +
                $"{nameof(flag_lastFeedbackGivenToParticpant)}: '{flag_lastFeedbackGivenToParticpant}'\n" +
                $"{nameof(flag_playerNeedsRespawn)}: '{flag_playerNeedsRespawn}'\n" +
                "\n" +

                $"{nameof(runningNumberOfPromptsCorrect)}: '{runningNumberOfPromptsCorrect}'\n" +
                $"{nameof(runningNumberOfFalseNegatives)}: '{runningNumberOfFalseNegatives}'\n" +
                $"{nameof(runningNumberOPromptsPresented)}: '{runningNumberOPromptsPresented}'\n" +
                $"{nameof(correctAnswerPercentage)}: '{correctAnswerPercentage}'\n" +
                $"{nameof(CorrectAnswerPercentage_presented)}: '{CorrectAnswerPercentage_presented}'\n" +

                $"";
#endif
        }

        public void EndStageAction_Countdown()
        {
            LogInc($"{nameof(EndStageAction_Countdown)}()");

            _audioSource_mainCamera.Play();

            NamruLogManager.DecrementTabLevel();
        }

        public void ChangeStageAction_SolvingPrompt()
        {
            LogInc( $"{nameof(ChangeStageAction_SolvingPrompt)}(). {nameof(index_currentPrompt)}: '{index_currentPrompt}'", LogDestination.Console );

            RingAnimators[Index_currentRing].SetBool("b_pulse", true);
            Flag_participantHasMadeChoice = false; //this is just a reset...
            flag_lastParticipantSelectionWasCorrect = false; //this is just a reset...
            flag_lastFeedbackGivenToParticpant = true; //this is just a reset
            runningNumberOPromptsPresented++;

            _inGameCanvasScript.SupplyPrompt();

            NamruLogManager.DecrementTabLevel();
        }

        public void AlarmEndAction_SolvingPromptStage()
        {
            if ( Flag_participantHasMadeChoice ) //just as a protective measure in case it somehow calls this even after a user has pressed a button...
            {
                return;
            }

            EvaluateChoice( false );
        }

        public void EvaluateChoice( bool correctChoiceSelected )
        {
            LogInc( $"{nameof(EvaluateChoice)}({correctChoiceSelected})" );
            flag_lastFeedbackGivenToParticpant = correctChoiceSelected; //this is important, because it makes this variable be whether the participant was correct by default.

            if ( correctChoiceSelected )
            {
                runningNumberOfPromptsCorrect++;
                flag_lastParticipantSelectionWasCorrect = true;

                if ( Random.Range(9, 100) <= 5 )
                {
                    Log( "False negative chosen", LogDestination.Everywhere );
                    runningNumberOfFalseNegatives++;

                    flag_lastFeedbackGivenToParticpant = false;
                }
            }

            correctAnswerPercentage = ((float)runningNumberOfPromptsCorrect / (float)runningNumberOPromptsPresented);
            CorrectAnswerPercentage_presented = ( (float)(runningNumberOfPromptsCorrect - runningNumberOfFalseNegatives) / (float)runningNumberOPromptsPresented );

            #region CALCULATE THE NEXT PROMPT DURATION----------------
            //float newCalculatedDuration = ScenarioManager.Instance.Stages[ScenarioIndex_SolvingPrompt].MyAlarm.Duration;
            float newCalculatedDuration = ScenarioManager.Instance.Stages[ScenarioIndex_SolvingPrompt].CurrentAlarm.Duration;

            if ( flag_lastParticipantSelectionWasCorrect && CorrectAnswerPercentage_presented > 0.6f )
            {
                newCalculatedDuration -= 0.06f;
            }
            else if( !flag_lastParticipantSelectionWasCorrect && correctAnswerPercentage < 0.4f )
            {
                newCalculatedDuration += 0.06f;
            }
            //ScenarioManager.Instance.Stages[ScenarioIndex_SolvingPrompt].MyAlarm.Duration = Mathf.Clamp( newCalculatedDuration, 1f, 3f );
            //ScenarioManager.Instance.Stages[ScenarioIndex_SolvingPrompt].CurrentAlarm.Duration = Mathf.Clamp(newCalculatedDuration, 1f, 3f);

            Log( $"{nameof(newCalculatedDuration)}: '{newCalculatedDuration}'" );

            #endregion

            _inGameCanvasScript.DisplayPromptFeedback( flag_lastFeedbackGivenToParticpant );

            index_currentPrompt++;
            if ( index_currentPrompt >= _PromptCollection.prompts.Length )
            {
                index_currentPrompt = 0;
            }

            ScenarioManager.Instance.GoToOrderedStage( ScenarioIndex_PromptAnsweredFeedback );

            NamruLogManager.DecrementTabLevel();
        }

        public void EndStageAction_PromptFeedback()
        {
            LogInc($"{nameof(EndStageAction_PromptFeedback)}()");

            if ( ScenarioManager.Instance._SessionState == NAMRUScenarioSystem.SessionState.Ended )
            {
                ScenarioManager.Instance.GoToOrderedStage(ScenarioIndex_Finished);
            }
            else
            {
                if( index_currentBlock < blockTimings.Length && ScenarioManager.Instance.RunningSessionDuration > blockTimings[index_currentBlock] )
                {
                    index_currentBlock++;
                    Log( $"reached new block '{index_currentBlock}'", LogDestination.Console );
                    flag_needToGoToBeratementStage = true;
                }

                if ( flag_lastFeedbackGivenToParticpant )
                {
                    if ( flag_needToGoToBeratementStage )
                    {
                        ScenarioManager.Instance.GoToOrderedStage( ScenarioIndex_Berating );

                    }
                    else
                    {
                        ScenarioManager.Instance.GoToOrderedStage( ScenarioIndex_SolvingPrompt );
                    }
                }
                else
                {
                    ScenarioManager.Instance.GoToOrderedStage( ScenarioIndex_RingIsExploding );
                }
            }

            NamruLogManager.DecrementTabLevel();
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
            catch ( System.Exception e )
            {
                NamruLogManager.LogException( e );
            }

            NamruLogManager.DecrementTabLevel();
        }

        public void AlarmEndAction_RingIsExploding()
        {
            LogInc( $"{nameof(AlarmEndAction_RingIsExploding)}(). {nameof(ScenarioManager.Instance._SessionState)}: '{ScenarioManager.Instance._SessionState}'. {nameof(Index_currentRing)}: '{Index_currentRing}'" );

            try
            {
                if( ScenarioManager.Instance._SessionState == NAMRUScenarioSystem.SessionState.Started ) //todo: is this right?
                {
                    if ( flag_needToGoToBeratementStage )
                    {
                        ScenarioManager.Instance.GoToOrderedStage( ScenarioIndex_Berating );
                        flag_needToGoToBeratementStage = false;
                    }
                    else
                    {
                        ScenarioManager.Instance.GoToOrderedStage( ScenarioIndex_SolvingPrompt );
                    }
                }
                else
                {
                    if ( Index_currentRing >= RingObjects.Length )
                    {
                        ScenarioManager.Instance.GoToOrderedStage( ScenarioIndex_Falling );
                    }
                    else
                    {
                        ScenarioManager.Instance.GoToOrderedStage( ScenarioIndex_Finished );
                    }
                }
            }
            catch ( System.Exception e )
            {
                NamruLogManager.LogException( e );

            }

            NamruLogManager.DecrementTabLevel();
        }

        public void ChangeStageAction_BeratingStage()
        {
            LogInc($"{nameof(ChangeStageAction_BeratingStage)}() placeholder. Audio needed...", LogDestination.Everywhere);



            NamruLogManager.DecrementTabLevel();
        }

        public void AlarmEndAction_beratingStage()
        {
            LogInc( $"{nameof(AlarmEndAction_beratingStage)}(). {nameof(Index_currentRing)}: '{Index_currentRing}'" );
            flag_needToGoToBeratementStage = false;

            if ( Index_currentRing >= RingObjects.Length )
            {
                ScenarioManager.Instance.GoToOrderedStage( ScenarioIndex_Falling );
            }
            else
            {
                ScenarioManager.Instance.GoToOrderedStage( ScenarioIndex_SolvingPrompt );
            }

            NamruLogManager.DecrementTabLevel();
        }

        public void ChangeStageAction_FallingStage()
        {
            LogInc( $"{nameof(ChangeStageAction_FallingStage)}()", LogDestination.Console );

            flag_playerNeedsRespawn = true;

            NamruLogManager.DecrementTabLevel();
        }

        public void ResetPlayerAndRingsAfterFall()
        {
            LogInc($"{nameof(ResetPlayerAndRingsAfterFall)}().");

            rb_player.useGravity = false;
            rb_player.velocity = Vector3.zero;
            rb_player.position = Vector3.zero;

            foreach ( ImvestRing ring in RingScripts )
            {
                ring.ResetMe();
            }

            Index_currentRing = 0;

            if ( ScenarioManager.Instance._SessionState == NAMRUScenarioSystem.SessionState.Ended )
            {
                ScenarioManager.Instance.GoToOrderedStage( ScenarioIndex_Finished );
            }
            else
            {
                ScenarioManager.Instance.GoToOrderedStage( ScenarioIndex_SolvingPrompt );
            }

            NamruLogManager.DecrementTabLevel();
        }

        public void ChangeToFinishedStage_action()
        {
            LogInc($"{nameof(ChangeToFinishedStage_action)}().", LogDestination.Console);

            RingAnimators[Index_currentRing].SetBool("b_pulse", false);

            NamruLogManager.DecrementTabLevel();
        }

        public void InputTrigger()//note: not using the trigger for selection anymore. Instead, they want directional buttons to instantly select
        {
            if ( ScenarioManager.Instance.Index_currentOrderedStage != ScenarioIndex_SolvingPrompt )
            {
                return;
            }

            _inGameCanvasScript.InputTrigger();
        }

        public void InputLeft()
        {
            if ( ScenarioManager.Instance.Index_currentOrderedStage != ScenarioIndex_SolvingPrompt )
            {
                return;
            }

            _inGameCanvasScript.InputLeft();
        }

        public void InputRight()
        {
            if ( ScenarioManager.Instance.Index_currentOrderedStage != ScenarioIndex_SolvingPrompt )
            {
                return;
            }

            _inGameCanvasScript.InputRight();
        }

        public bool CheckIfKosher()
        {
            bool amKosher = true;

            if ( _PromptCollection == null)
            {
                amKosher = false;
                LogError($"{nameof(_PromptCollection)} reference was null!");
            }
            else if( _PromptCollection.prompts == null )
            {
                amKosher = false;
                LogError($"{nameof(_PromptCollection.prompts)} collection was null!");
            }
            else if ( _PromptCollection.prompts.Length <= 0 )
            {
                amKosher = false;
                LogError($"{nameof(_PromptCollection.prompts)} collection length was {_PromptCollection.prompts.Length}!");
            }

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

            if ( ScenarioManager.Instance.Stages[ScenarioIndex_WaitingToBegin]._description != "Waiting To Begin" )
            {
                amKosher = false;
                LogError($"{nameof(ScenarioIndex_WaitingToBegin)} description didn't match what was expected. Did the stages change?");
            }

            if ( ScenarioManager.Instance.Stages[ScenarioIndex_CountdownToStart]._description != "Countdown To Start" )
            {
                amKosher = false;
                LogError($"{nameof(ScenarioIndex_CountdownToStart)} description didn't match what was expected. Did the stages change?");
            }

            if (ScenarioManager.Instance.Stages[ScenarioIndex_SolvingPrompt]._description != "Solving Prompt")
            {
                amKosher = false;
                LogError($"{nameof(ScenarioIndex_SolvingPrompt)} description didn't match what was expected. Did the stages change?");
            }

            if ( ScenarioManager.Instance.Stages[ScenarioIndex_PromptAnsweredFeedback]._description != "Prompt Answered Feedback")
            {
                amKosher = false;
                LogError($"{nameof(ScenarioIndex_PromptAnsweredFeedback)} description didn't match what was expected. Did the stages change?");
            }

            if ( ScenarioManager.Instance.Stages[ScenarioIndex_RingIsExploding]._description != "Ring Is Exploding")
            {
                amKosher = false;
                LogError($"{nameof(ScenarioIndex_RingIsExploding)} description didn't match what was expected. Did the stages change?");
            }

            if (ScenarioManager.Instance.Stages[ScenarioIndex_Falling]._description != "Falling")
            {
                amKosher = false;
                LogError($"{nameof(ScenarioIndex_Falling)} description didn't match what was expected. Did the stages change?");
            }

            if (ScenarioManager.Instance.Stages[ScenarioIndex_Berating]._description != "Berating")
            {
                amKosher = false;
                LogError($"{nameof(ScenarioIndex_Berating)} description didn't match what was expected. Did the stages change?");
            }

            if (ScenarioManager.Instance.Stages[ScenarioIndex_Finished]._description != "Finished")
            {
                amKosher = false;
                LogError($"{nameof(ScenarioIndex_Finished)} description didn't match what was expected. Did the stages change?");
            }

            /*if( ScenarioManager.Instance.Stages[ScenarioIndex_SolvingPrompt].MyAlarm.Duration != 2.5f )
            {
                amKosher = false;
                LogError($"Solving prompt stage has a duration of '{ScenarioManager.Instance.Stages[ScenarioIndex_SolvingPrompt].MyAlarm.Duration}, " +
                    $"which doesn't appear to be correct'");
            }

            if ( ScenarioManager.Instance.Stages[ScenarioIndex_PromptAnsweredFeedback].MyAlarm.Duration != 1.75f )
            {
                amKosher = false;
                LogError($"Prompt feedback stage has a duration of '{ScenarioManager.Instance.Stages[ScenarioIndex_PromptAnsweredFeedback].MyAlarm.Duration}, " +
                    $"which doesn't appear to be correct'");
            }*/

            if (_audioSource_mainCamera == null)
            {
                amKosher = false;
                LogError("couldn't find main camera audio source");
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