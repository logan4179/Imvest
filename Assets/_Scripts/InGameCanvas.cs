using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NamruSessionManagementSystem;
using UnityEngine.EventSystems;

namespace OldWay
{
    public class InGameCanvas : NSMS_Object
    {
        [Header("[---- REFERENCE (INTERNAL) ----]")]
        [SerializeField] private TextMeshProUGUI txt_prompt;
        [SerializeField] private TextMeshProUGUI txt_timer;
        [SerializeField] private TextMeshProUGUI txt_score;
        [SerializeField] private TextMeshProUGUI txt_goWhenReady;

        [SerializeField] private Button btn_Go;

        [SerializeField] private GameObject GmOb_PromptGroup;
        [SerializeField] private GameObject GmOb_ReadyGroup;
        [SerializeField] private GameObject GmOb_choicesGroup;
        [SerializeField] private Button[] btns_choices;
        [SerializeField] private TextMeshProUGUI[] btns_choices_txts;

        [Header("[---- REFERENCE (EXTERNAL) ----]")]
        [SerializeField] private Rigidbody rb_player;

        //[Header("[---- OTHER ----]")]
        /// <summary>
        /// Gets set to the index of the choice button that has the correct answer each prompt.
        /// </summary>
        private int index_correctButton = 0;

        /// <summary>
        /// Keeps track of which button was last selected so it knows which one to highlight each time
        /// </summary>
        private int index_lastSelectedButton = 0;

        [Header("[---- DEBUG ----]")]
        [SerializeField] private string DBGstring;
        [SerializeField] private GameObject group_debug;
        [SerializeField] private TextMeshProUGUI txt_debug;

        void Start()
        {
            LogInc("Start()");
            CheckIfKosher();

            txt_timer.text = "";
            txt_score.text = "";
            GmOb_PromptGroup.SetActive(false);
            rb_player.useGravity = false;

            GmOb_ReadyGroup.SetActive(true);

            if (!Application.isEditor)
            {
                group_debug.SetActive(false);
            }

            EventSystem.current.SetSelectedGameObject(btns_choices[1].gameObject);
            

            NamruLogManager.DecrementTabLevel();
        }

        void Update()
        {
            if (GameManager.CurrentGameState == GameState.WaitingToBegin)
            {
                if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Space))
                {
                    UI_Btn_Go_action();
                }
            }
            else if (GameManager.CurrentGameState == GameState.CountdownToStart)
            {
                txt_goWhenReady.text = ((int)(GameManager.Instance.Alarm_cd_countingDownBeforeFirstPrompt.CurrentValue + 1)).ToString();
            }
            else if (GameManager.CurrentGameState == GameState.SolvingPrompt)
            {
                //txt_timer.text = $"Time left: {(int)(GameManager.Instance.Alarm_cd_answeringPrompt.CurrentValue + 1)}"; //if I want whole numbers
                txt_timer.text = $"Time left: {GameManager.Instance.Alarm_cd_answeringPrompt.CurrentValue.ToString("#.#")}";
            }
            else if (GameManager.CurrentGameState == GameState.PromptSolvedFeedback)
            {

            }
            else if (GameManager.CurrentGameState == GameState.RingIsExploding)
            {

            }
            else if (GameManager.CurrentGameState == GameState.Falling)
            {

            }

#if UNITY_EDITOR
            DBGstring = $"GAME MANAGER--------------------\n" +
                GameManager.Instance.DBGstring + "\n\n" +

                "Canvas-----------------------------------\n" +
                $"{nameof(index_correctButton)}: '{index_correctButton}\n\n" +
                $"";

            txt_debug.text = DBGstring;
#endif

        }

        public void ChangeState(GameState state, GameState previousState)
        {
            LogInc($"{nameof(ChangeState)}({state})");

            if (state == GameState.CountdownToStart)
            {
                //cd_toStart = 3f; //todo: dws
                btn_Go.gameObject.SetActive(false);
                GmOb_PromptGroup.SetActive(false);
                txt_prompt.gameObject.SetActive(true);
            }
            else if (state == GameState.SolvingPrompt)
            {
                GmOb_ReadyGroup.SetActive(false);
                GmOb_PromptGroup.SetActive(true);
                GmOb_choicesGroup.SetActive(true); //This is necessary becasue this group is turned off for the feedback state.
                txt_prompt.text = GameManager.Instance._PromptCollection.prompts[GameManager.Instance.index_currentPrompt].formattedQuestion;

                if (previousState == GameState.CountdownToStart)
                {
                    txt_score.text = "Score: 0";
                }

                #region Update Prompting -----------------------------
                txt_prompt.text = GameManager.Instance._PromptCollection.prompts[GameManager.Instance.index_currentPrompt].formattedQuestion;

                index_correctButton = Random.Range(0, btns_choices.Length - 1);
                btns_choices_txts[index_correctButton].text = GameManager.Instance.CurrentPrompt.correctAnswer.ToString();

                if (index_correctButton == 0)
                {
                    btns_choices_txts[1].text = GameManager.Instance.CurrentPrompt.incorrectAnswerA.ToString();
                    btns_choices_txts[2].text = GameManager.Instance.CurrentPrompt.incorrectAnswerB.ToString();
                }
                else if (index_correctButton == 1)
                {
                    btns_choices_txts[0].text = GameManager.Instance.CurrentPrompt.incorrectAnswerA.ToString();
                    btns_choices_txts[2].text = GameManager.Instance.CurrentPrompt.incorrectAnswerB.ToString();
                }
                else if (index_correctButton == 2)
                {
                    btns_choices_txts[0].text = GameManager.Instance.CurrentPrompt.incorrectAnswerA.ToString();
                    btns_choices_txts[1].text = GameManager.Instance.CurrentPrompt.incorrectAnswerB.ToString();
                }
                #endregion

                EventSystem.current.SetSelectedGameObject(btns_choices[index_lastSelectedButton].gameObject);

            }
            else if (state == GameState.PromptSolvedFeedback)
            {
                GmOb_choicesGroup.SetActive(false);
                txt_timer.text = "";
            }
            else if (state == GameState.RingIsExploding)
            {
                GmOb_PromptGroup.SetActive(false);
            }
            else if (state == GameState.Falling)
            {
                GmOb_PromptGroup.SetActive(false);
                txt_timer.text = "";
            }
            else if (state == GameState.Finished)
            {
                GmOb_PromptGroup.SetActive(false);
                GmOb_ReadyGroup.SetActive(true);
                txt_goWhenReady.text = "Finished"; //todo: why doesn't this happen when you fall and go below the threshold?
                txt_timer.text = "";
            }

            NamruLogManager.DecrementTabLevel();
        }

        #region UI ---------------------------------------------------------------
        [ContextMenu("z call UI_Btn_Go_action()")]
        public void UI_Btn_Go_action()
        {
            LogInc($"{nameof(UI_Btn_Go_action)}()");

            GmOb_PromptGroup.SetActive(false);

            GameManager.Instance.ChangeState(GameState.CountdownToStart);

            NamruLogManager.DecrementTabLevel();
        }

        public void UI_Btn1_action()
        {
            LogInc($"{nameof(UI_Btn1_action)}()");

            index_lastSelectedButton = 0;
            ChoiceMade_action(index_correctButton == 0);

            GameManager.Instance.flag_haveMadeSelection = true;

            NamruLogManager.DecrementTabLevel();
        }

        public void UI_Btn2_action()
        {
            LogInc($"{nameof(UI_Btn2_action)}()");

            index_lastSelectedButton = 1;
            ChoiceMade_action(index_correctButton == 1);

            GameManager.Instance.flag_haveMadeSelection = true;

            NamruLogManager.DecrementTabLevel();
        }

        public void UI_Btn3_action()
        {
            LogInc($"{nameof(UI_Btn3_action)}()");

            index_lastSelectedButton = 2;
            ChoiceMade_action(index_correctButton == 2);

            GameManager.Instance.flag_haveMadeSelection = true;

            NamruLogManager.DecrementTabLevel();
        }
        #endregion

        public void ChoiceMade_action(bool correctChoiceSelected)
        {
            LogInc($"{nameof(ChoiceMade_action)}({correctChoiceSelected})");
            if (correctChoiceSelected)
            {
                GameManager.Instance.runningScore += GameManager.Instance.amount_pointsPerPrompt;
                txt_prompt.text = "<color=green>Correct!</color>";
                GameManager.Instance.flag_lastSelectionWasCorrect = true;
            }
            else
            {
                GameManager.Instance.runningScore -= GameManager.Instance.amount_pointsPerPrompt;
                txt_prompt.text = "<color=red>Incorrect!</color>";

            }

            txt_score.text = $"Score: {GameManager.Instance.runningScore}";
            GameManager.Instance.ChangeState(GameState.PromptSolvedFeedback);

            NamruLogManager.DecrementTabLevel();
        }

        public void InputTrigger()
        {
            NamruLogManager.Log( $"{nameof(InputTrigger)}()", LogDestination.MomentaryDebugLogger );
            
            if( index_lastSelectedButton == 0 )
            {
                UI_Btn1_action();
            }
            else if( index_lastSelectedButton == 1 )
            {
                UI_Btn2_action();
            }
            else if ( index_lastSelectedButton == 2 )
            {
                UI_Btn3_action();
            }
        }

        public void InputLeft()
        {
            index_lastSelectedButton--;
            if( index_lastSelectedButton < 0 )
            {
                index_lastSelectedButton = 0;
            }


            EventSystem.current.SetSelectedGameObject(btns_choices[index_lastSelectedButton].gameObject);
        }

        public void InputRight()
        {
            index_lastSelectedButton++;
            if ( index_lastSelectedButton > btns_choices.Length-1 )
            {
                index_lastSelectedButton = btns_choices.Length-1;
            }


            EventSystem.current.SetSelectedGameObject(btns_choices[index_lastSelectedButton].gameObject);
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

            if (btn_Go == null)
            {
                amKosher = false;
                LogError($"{nameof(btn_Go)} reference was null!");
            }

            if (GmOb_PromptGroup == null)
            {
                amKosher = false;
                LogError($"{nameof(GmOb_PromptGroup)} reference was null!");
            }

            if (GmOb_ReadyGroup == null)
            {
                amKosher = false;
                LogError($"{nameof(GmOb_ReadyGroup)} reference was null!");
            }

            if (GmOb_choicesGroup == null)
            {
                amKosher = false;
                LogError($"{nameof(GmOb_choicesGroup)} reference was null!");
            }

            if (btns_choices == null || btns_choices.Length <= 0)
            {
                amKosher = false;
                LogError($"{nameof(btns_choices)} reference was null or had 0 values!");
            }

            if (rb_player == null)
            {
                amKosher = false;
                LogError($"{nameof(rb_player)} reference was null!");
            }

            return amKosher;
        }
    }
}