using System.Collections.Generic;

using Saaft.Data.Auditing;

namespace Saaft.Data.Accounts
{
    public static class Auditing
    {
        public static readonly AuditedActionCategoryEntity ActionCategory
            = new()
            {
                Id      = 0xABF962CA,
                Name    = "Account Management"
            };

        public static class ActionTypes
        {
            public static readonly AuditedActionTypeEntity AccountCreated
                = new()
                {
                    Id          = 0xFDD34A8F,
                    CategoryId  = ActionCategory.Id,
                    Name        = "Account Created"
                };

            public static readonly AuditedActionTypeEntity AccountMutated
                = new()
                {
                    Id          = 0x9D31D1ED,
                    CategoryId  = ActionCategory.Id,
                    Name        = "Account Edited"
                };

            public static IEnumerable<AuditedActionTypeEntity> Enumerate()
            {
                yield return AccountCreated;
                yield return AccountMutated;
            }
        }
    }
}
