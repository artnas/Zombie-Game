using System.Collections.Generic;
using Enemy.States;
using UnityEngine;

namespace Enemy
{
    public static class ZombieUtilities
    {
        public static Dictionary<string, ZombieState> GetStatesDictionary(List<ZombieState> list)
        {
            var result = new Dictionary<string, ZombieState>();

            foreach (var state in list)
            {
                result.Add(state.GetName(), state);
            }
            
            return result;
        }
    }
}