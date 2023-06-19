using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

//This code has been taken from the page: https://answers.unity.com/questions/1650130/change-agenttype-at-runtime.html
//All credits to the person in the link above
public static class AgentTypeID
{
    public static int GetAgenTypeIDByName(string agentTypeName)
    {
        int count = NavMesh.GetSettingsCount();
        string[] agentTypeNames = new string[count + 2];
        for (var i = 0; i < count; i++)
        {
            int id = NavMesh.GetSettingsByIndex(i).agentTypeID;
            string name = NavMesh.GetSettingsNameFromID(id);
            if (name == agentTypeName)
            {
                return id;
            }
        }
        return -1;
    }
}
