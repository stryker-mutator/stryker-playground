using System;
using System.Collections.Generic;
using System.Text;

namespace Stryker
{
    public static class MutantControl
    {
        // check with: Stryker.MutantControl.IsActive(ID)
        public static bool IsActive(int id)
        {
            RegisterMutationAsCovered(id);
            return id == GetActiveMutation();
        }

        public static void RegisterMutationAsCovered(int id)
        {
            string environmentVariable = Environment.GetEnvironmentVariable("CoveredMutations") ?? string.Empty;
            
            Environment.SetEnvironmentVariable("CoveredMutations", environmentVariable + "," + id);
        }

        public static int GetActiveMutation()
        {
            string environmentVariable = Environment.GetEnvironmentVariable("ActiveMutation");
            if (string.IsNullOrEmpty(environmentVariable))
            {
                return -1;
            }
            else
            {
                return int.Parse(environmentVariable);
            }
        }
    }
}
