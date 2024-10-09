using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NamruSessionManagementSystem;
using NAMRUScenarioSystem;
using UnityEngine.EventSystems;
using Varjo.XR;

namespace NewWay
{
    public class InGameCanvas_new : NSMS_Object
    {
        [Header("[---- REFERENCE (INTERNAL) ----]")]
        [SerializeField] private TextMeshProUGUI txt_prompt;
        [SerializeField] private TextMeshProUGUI txt_timer;
        [SerializeField] private TextMeshProUGUI txt_score;
        [SerializeField] private TextMeshProUGUI txt_goWhenReady;

        [SerializeField] private Button btn_true;
        [SerializeField] private Button btn_false;

        //[Header("[---- OTHER ----]")]
        /// <summary>
        /// true, if the true button was last selected, false if the false button was last selected.
        /// </summary>
        private bool lastSelectedOption;

        [Header("[---- DEBUG ----]")]
        [SerializeField] private string DBGstring;
        [SerializeField] private GameObject group_debug;
        [SerializeField] private TextMeshProUGUI txt_debug;

        void Start()
        {
            LogInc("Start()");

            CheckIfKosher();

            if (!Application.isEditor)
            {
                group_debug.SetActive(false);
            }

            txt_score.text = "Score: 0%";

            NamruLogManager.DecrementTabLevel();
        }

        
        void Update()
        {
            if (ScenarioManager.Instance.Index_currentOrderedStage == GameManager_new.ScenarioIndex_WaitingToBegin)
            {
                /*if ( Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Space) ) 
                {
                    UI_Btn_Go_action();
                }*/
            }
            else if (ScenarioManager.Instance.Index_currentOrderedStage == GameManager_new.ScenarioIndex_CountdownToStart)
            {
                /*
                txt_goWhenReady.text = (
                    (int)(ScenarioManager.Instance.CurrentStage.MyAlarm.CurrentValue + 1)
                ).ToString();
                */

                txt_goWhenReady.text = (
                    (int)(ScenarioManager.Instance.CurrentStage.CurrentAlarm.CurrentValue + 1)
                ).ToString();
            }
            else if (ScenarioManager.Instance.Index_currentOrderedStage == GameManager_new.ScenarioIndex_SolvingPrompt)
            {
                //txt_timer.text = $"Time left: {(int)(GameManager_new.Instance.Alarm_cd_answeringPrompt.CurrentValue + 1)}"; //if I want whole numbers
                //txt_timer.text = $"Time left: {ScenarioManager.Instance.CurrentStage.MyAlarm.CurrentValue.ToString("#.#")}";
                txt_timer.text = $"Time left: {ScenarioManager.Instance.CurrentStage.CurrentAlarm.CurrentValue.ToString("#.#")}";

            }
            else if (ScenarioManager.Instance.Index_currentOrderedStage == GameManager_new.ScenarioIndex_PromptAnsweredFeedback)
            {

            }
            else if (ScenarioManager.Instance.Index_currentOrderedStage == GameManager_new.ScenarioIndex_RingIsExploding)
            {

            }
            else if (ScenarioManager.Instance.Index_currentOrderedStage == GameManager_new.ScenarioIndex_Falling)
            {

            }

#if UNITY_EDITOR
            DBGstring = GameManager_new.Instance.DBGstring + "\n\n" +

                "<b><u>Canvas</u></b>\n" +
                $"{nameof(lastSelectedOption)}: '{lastSelectedOption}\n\n" +

                "<b><u>Headset</u></b>\n" +
                $"ipd: '{VarjoHeadsetIPD.GetDistance()}'\n" +
                $"";

            txt_debug.text = DBGstring;
#endif

        }

        public void SupplyPrompt()
        {
            LogInc($"{nameof(SupplyPrompt)}(). {nameof(lastSelectedOption)}: '{lastSelectedOption}'");

            try
            {
                txt_prompt.text = GameManager_new.Instance._PromptCollection.prompts[GameManager_new.Instance.index_currentPrompt].formattedQuestion;

                EventSystem.current.SetSelectedGameObject( lastSelectedOption == true ? btn_true.gameObject : btn_false.gameObject );
            }
            catch ( System.Exception e )
            {
                LogInc( $"{nameof(SupplyPrompt)}(). {nameof(lastSelectedOption)}: '{lastSelectedOption}' index: '{GameManager_new.Instance.index_currentPrompt}'", LogDestination.Console );
                NamruLogManager.LogException( e );
                throw;
            }


            NamruLogManager.DecrementTabLevel();
        }

        #region UI ---------------------------------------------------------------
        [ContextMenu("z call UI_Btn_Go_action()")]
        public void UI_Btn_Go_action()
        {
            LogInc( $"{nameof(UI_Btn_Go_action)}()" );

            try
            {
                ScenarioManager.Instance.AdvanceStage();

            }
            catch (System.Exception e)
            {
                NamruLogManager.LogException(e);
                //throw;
            }

            NamruLogManager.DecrementTabLevel();
        }

        public void UI_ChoiceButton_action(bool choice)
        {
            LogInc($"{nameof(UI_ChoiceButton_action)}('{choice}')");

            GameManager_new.Instance.Flag_participantHasMadeChoice = true;
            lastSelectedOption = choice;
            GameManager_new.Instance.EvaluateChoice( GameManager_new.Instance.CurrentPrompt.answer == choice );

            NamruLogManager.DecrementTabLevel();
        }
        #endregion

        public void DisplayPromptFeedback( bool correctChoicePresented )
        {
            LogInc($"{nameof(DisplayPromptFeedback)}({correctChoicePresented}) presented score: '{GameManager_new.Instance.CorrectAnswerPercentage_presented}'", 
                LogDestination.Hidden );

            if (correctChoicePresented)
            {
                txt_prompt.text = "<color=green>Correct!</color>";
            }
            else
            {
                txt_prompt.text = "<color=red>Incorrect!</color>";

            }

            string scoreText = "";
            if ( GameManager_new.Instance.CorrectAnswerPercentage_presented < GameManager_new.AnswerPercentageThreshold_presentedAsAverage )
            {
                if ( GameManager_new.Instance.CorrectAnswerPercentage_presented == 0 )
                {
                    scoreText = $"<color=red>Score: 0%</color>"; //For some reason, I couldn't get it to display 0 without doing this...
                }
                else
                {
                    scoreText = $"<color=red>Score: {(GameManager_new.Instance.CorrectAnswerPercentage_presented * 100).ToString("#.#")}%</color>";
                }
            }
            else
            {
                scoreText = $"<color=green>Score: {(GameManager_new.Instance.CorrectAnswerPercentage_presented * 100).ToString("#.#")}%</color>";
            }

            txt_score.text = scoreText;

            NamruLogManager.DecrementTabLevel();
        }

        public void InputTrigger() //note: not using the trigger for selection anymore. Instead, they want directional buttons to instantly select
        {
            Log($"{nameof(InputTrigger)}()", LogDestination.MomentaryDebugLogger);

            UI_ChoiceButton_action( lastSelectedOption );

        }

        public void InputLeft()
        {
            if( lastSelectedOption == false )
            {
                lastSelectedOption = true;
            }

            UI_ChoiceButton_action(lastSelectedOption);

        }

        public void InputRight()
        {
            if ( lastSelectedOption == true )
            {
                lastSelectedOption = false;
            }

            UI_ChoiceButton_action(lastSelectedOption);

        }

        public bool CheckIfKosher()
        {
            bool amKosher = true;

            if (txt_prompt == null)
            {
                amKosher = false;
                LogError($"{nameof(txt_prompt)} reference was null!");
            }

            if (txt_timer == null)
            {
                amKosher = false;
                LogError($"{nameof(txt_timer)} reference was null!");
            }

            if (txt_goWhenReady == null)
            {
                amKosher = false;
                LogError($"{nameof(txt_goWhenReady)} reference was null!");
            }

            if (txt_score == null)
            {
                amKosher = false;
                Debug.LogError($"{nameof(txt_score)} reference was null!");
            }

            if ( btn_true == null )
            {
                amKosher = false;
                LogError($"{nameof(btn_true)} reference was null!");
            }

            if ( btn_false == null )
            {
                amKosher = false;
                LogError($"{nameof(btn_false)} reference was null!");
            }

            return amKosher;
        }
    }
}