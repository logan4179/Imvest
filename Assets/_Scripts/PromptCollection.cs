using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MVEST_Objects/Prompts", fileName = "prompts")]

public class PromptCollection : ScriptableObject
{
    //public string[] prompts;
    public ImvestPrompt[] prompts;

    public string muhString;

    [ContextMenu("z call FormatQuestionTexts()")]
    public void FormatQuestionTexts()
    {

        
    }

    [ContextMenu("z call SolvePrompts()")]
    public void SolvePrompts()
    {
        if( prompts == null || prompts.Length <= 0 )
        {
            Debug.LogError( $"prompts is either null or 0 length. Returning early..." );
            return;
        }

        Debug.Log( $"Solving {prompts.Length} prompts...");

        foreach ( ImvestPrompt prompt in prompts )
        {
            prompt.SolveMe();
        }

        muhString = prompts[0].formattedQuestion;
        muhString = "stranga";
    }
}
