using Flower.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Core.Abstractions.Services
{
    public interface IShowPlayerService
    {
        Task PlayAsync(ShowProject project);

        void Stop();
    }
}

