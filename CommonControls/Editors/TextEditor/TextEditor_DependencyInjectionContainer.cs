﻿using CommonControls.Common;
using Microsoft.Extensions.DependencyInjection;

namespace CommonControls.Editors.TextEditor
{
    public class TextEditor_DependencyInjectionContainer
    {
        public static void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<TextEditorView>();
            serviceCollection.AddTransient<DefaultTextConverter>();
            serviceCollection.AddTransient<TextEditorViewModel<DefaultTextConverter>>();
        }

        public static void RegisterTools(IToolFactory factory)
        {
            factory.RegisterTool<TextEditorViewModel<DefaultTextConverter>, TextEditorView>(new ExtentionToTool(".json", ".xml", ".txt", ".wsmodel", ".xml.material", ".anim.meta.xml", ".anm.meta.xml", ".snd.meta.xml", ".bmd.xml", ".csv" ));
        }
    }
}
