using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NAMRUScenarioSystem
{
    [System.Serializable]
    public struct StageInput
    {
        public KeyCode _KeyCode;

        [Tooltip("0 = GetKeyDown, 1 = GetKey, 2 = GetKeyUp")] public int PressMode;

        public bool AmBeingTriggered()
        {
            if (PressMode == 0 && Input.GetKeyDown(_KeyCode))
            {
                return true;
            }
            else if (PressMode == 1 && Input.GetKey(_KeyCode))
            {
                return true;
            }
            else if (PressMode == 2 && Input.GetKeyUp(_KeyCode))
            {
                return true;
            }

            return false;
        }
    }
}