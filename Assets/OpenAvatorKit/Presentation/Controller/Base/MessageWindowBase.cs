using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace OpenAvatarKit.Presentation.Controller.Base
{
    public interface IMessageWindow
    {
        void Show(string prompt = null);
        void Hide();
        UniTask ShowMessageAsync(string message, CancellationToken token);
        UniTask SetMessageAsync(string message, CancellationToken token);
    }

    public class MessageWindowBase : MonoBehaviour, IMessageWindow
    {
        public bool IsInstance = true;

        public virtual void Show(string prompt = null)
        {
            throw new NotImplementedException();
        }

        public virtual void Hide()
        {
            throw new NotImplementedException();
        }

        public virtual UniTask ShowMessageAsync(string message, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public virtual UniTask SetMessageAsync(string message, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
