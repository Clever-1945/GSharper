using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSharper.Models
{
    public class GoToResult
    {
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }

        public GoToResult(bool isSuccess, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }
    }
}
