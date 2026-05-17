using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GSharper.QuickInfo
{
    //[Order(Before = "RoslynQuickInfoProvider")]
    // [ContentType("CSharp")]

    //[Export(typeof(IAsyncQuickInfoSourceProvider))]
    //[Name(nameof(QuickInfoController))]
    //[Order(After = "Default Quick Info Presenter")]
    //[ContentType("Text")]

    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name(nameof(QuickInfoController))]
    [Order(Before = "Default")]
    [ContentType("CSharp")]
    sealed class QuickInfoControllerProvider : IAsyncQuickInfoSourceProvider
    {
        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new QuickInfoController());
        }
    }


    [Export(typeof(IViewElementFactory))]
    [Name("InfoBlock Quick Info Factory")]
    [TypeConversion(from: typeof(QuickInfoBlock), to: typeof(System.Windows.UIElement))]
    [Order(Before = "Default object converter")]
    public class QuickInfoBlockQuickInfoFactory : IViewElementFactory
    {
        public TView CreateViewElement<TView>(ITextView textView, object model) where TView : class
        {
            return (model as QuickInfoBlock)?.ToUI() as TView;
        }
    }

    [Export(typeof(IIntellisenseControllerProvider))]
    [Name("CustomQuickInfoController")]
    [ContentType("CSharp")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class MyControllerProvider : IIntellisenseControllerProvider
    {
        [Import]
        internal IAsyncQuickInfoBroker QuickInfoBroker { get; set; }

        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers)
        {
            return new MyQuickInfoController(textView, QuickInfoBroker);
        }
    }

    internal class MyQuickInfoController : IIntellisenseController
    {
        private ITextView _textView;
        private IAsyncQuickInfoBroker _broker;

        public MyQuickInfoController(ITextView textView, IAsyncQuickInfoBroker broker)
        {
            _textView = textView;
            _broker = broker;
            // Подписываемся на событие "мышь замерла"
            _textView.MouseHover += OnMouseHover;
        }

        private async void OnMouseHover(object sender, MouseHoverEventArgs e)
        {
            // Проверяем, не запущена ли уже сессия, чтобы не спамить
            if (!_broker.IsQuickInfoActive(_textView))
            {
                var triggerPoint = _textView.BufferGraph.MapDownToFirstMatch(
                    new SnapshotPoint(_textView.TextSnapshot, e.Position),
                    PointTrackingMode.Positive,
                    snapshot => true,
                    PositionAffinity.Predecessor);

                if (triggerPoint.HasValue)
                {
                    var trackingPoint = triggerPoint.Value.Snapshot.CreateTrackingPoint(triggerPoint.Value.Position, PointTrackingMode.Positive);
                    if (trackingPoint != null)
                    {
                        var activeSession = _broker.GetSession(_textView);
                        if (activeSession != null)
                        {
                            //await activeSession.DismissAsync();
                        }
                        await Task.Delay(100);
                        var session = await _broker.TriggerQuickInfoAsync(_textView, trackingPoint, QuickInfoSessionOptions.None);
                    }
                }
            }
        }

        public void Detach(ITextView textView) { _textView.MouseHover -= OnMouseHover; }
        public void ConnectSubjectBuffer(ITextBuffer subjectBuffer) { }
        public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer) { }
    }
}
