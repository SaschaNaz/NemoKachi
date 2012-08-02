using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NemoKachi
{
    public class AccountTokenCollector
    {
        public ObservableCollection<TwitterWrapper.AccountToken> TokenCollection { get; private set; }

        public AccountTokenCollector()
        {
            TokenCollection = new ObservableCollection<TwitterWrapper.AccountToken>();
        }

        public TwitterWrapper.AccountToken GetTokenByID(UInt64 Id)
        {
            foreach (TwitterWrapper.AccountToken aToken in TokenCollection)
                if (aToken.AccountId == Id)
                    return aToken;
            return null;
        }
    }
}
