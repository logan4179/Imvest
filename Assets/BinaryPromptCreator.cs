using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class BinaryPromptCreator : MonoBehaviour
{
    [SerializeField] BinaryPromptCollection _collection;

    [SerializeField, TextArea(1,10)] private string _collectionText;

    [SerializeField] private string[] lines;

    [ContextMenu("z call ParseTextAndSetUpCollection()")]
    public void ParseTextAndSetUpCollection()
    {
#if UNITY_EDITOR

        //string[] promptObjects = _collectionText.Split( Environment.NewLine ); //this doesn't split properly
        //string[] promptObjects = _collectionText.Split("\r"); //NO

        //string[] promptObjects = Regex.Split(_collectionText, "\r\n|\r|\n"); // this works
        lines = _collectionText.Split("\n"); //this works.

        if ( lines.Length < 5 )
        {
            Debug.LogError($"There seems to be a formatting problem, as only '{lines.Length}' results were split from this text!");
            return;
        }

        print($"found '{lines.Length}' prompt objects");

        _collection.prompts = new BinaryPrompt[ lines.Length ];
        for ( int i = 0; i < lines.Length; i++ )
        {
            if ( lines[i].Contains("\t") )
            {
                string[] tabSplitString = lines[i].Split("\t"); //always resulted in length of 5 with current csv I was given
                print($"found '{tabSplitString.Length}' tab splits. first: '{tabSplitString[0]}', second: '{tabSplitString[1]}'");
                
                _collection.prompts[i].formattedQuestion = tabSplitString[0];

                //if( string.IsNullOrEmpty(tabSplitString[4]) ) //note: this didn't work even though some of the seemed empty
                //if ( string.IsNullOrWhiteSpace(tabSplitString[1]) ) //note this seems to work. note: not using this format anymore...
                //if( tabSplitString[1] == "Correct" ) //didn't work
                string kosherString = ImvestUtils.MakeStringKosher( tabSplitString[1] );
                if ( kosherString == "Correct" )
                {
                    _collection.prompts[i].answer = true;
                }
                else if(kosherString == "Incorrect" )
                {
                    _collection.prompts[i].answer = false;
                }
                else
                {
                    Debug.LogError( $"tab-split string for '{lines[i]}' second collumn says '{tabSplitString[1]}', which wasn't paresable" );
                }
            }
            else
            {
                Debug.LogError( $"entry number {i}: '{lines[i]}' didn't contain tabs. Couldn't properly parse..." );
            }
            //print($"parsing '{results[i]}', found '{tabNumber}' tabs");
        }

        UnityEditor.EditorUtility.SetDirty( _collection ); //This was what fixed it

#endif


    }
}
