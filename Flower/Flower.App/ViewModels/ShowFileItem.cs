using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.App.ViewModels
{
    public sealed class ShowFileItem
    {
        public string Path { get; }
        public string DisplayName { get; }

        public ShowFileItem(string path)
        {
            Path = path;
            DisplayName = System.IO.Path.GetFileName(path);
        }
    }
}
