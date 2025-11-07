using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Abstractions.Services
{
    public interface IUiLogService
    {
        event Action<string> InfoPublished;
        event Action<string, Exception> ErrorPublished;

        void Info(string message);
        void Error(string message, System.Exception ex);
    }
}
