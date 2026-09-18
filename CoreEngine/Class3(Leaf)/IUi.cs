using System;
using System.Collections.Generic;
using System.Text;
namespace CoreEngine
{
    public interface IUi : IModule
    {
        public void Show();
        public void Hide();
    }
}
