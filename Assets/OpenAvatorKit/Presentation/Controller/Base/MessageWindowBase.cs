using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace OpenAvatarKid.Presentation.Controllers.Base
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
            throw new System.NotImplementedException();
        }

        public virtual void Hide()
        {
            throw new System.NotImplementedException();
        }

        public virtual UniTask ShowMessageAsync(string message, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }

        public virtual UniTask SetMessageAsync(string message, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }
    }
}
