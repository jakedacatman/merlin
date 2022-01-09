using System;
using Fergun.Interactive;
using Fergun.Interactive.Selection;
using Discord;

namespace merlin.classes //https://github.com/d4n3436/Fergun.Interactive/blob/master/ExampleBot/Modules/CustomButtonModule.cs
{
    public class ButtonSelectionBuilder<T> : BaseSelectionBuilder<ButtonSelection<T>, ButtonOption<T>, ButtonSelectionBuilder<T>>
    {
        // Since this is ButtonSelection is specifically created for buttons, it makes sense to make this option the default.
        public override InputType InputType => InputType.Buttons;

        // We must override the Build method
        public override ButtonSelection<T> Build() => new(this);
    }

    // Custom selection where you can override the default button style/color
    public class ButtonSelection<T> : BaseSelection<ButtonOption<T>>
    {
        public ButtonSelection(ButtonSelectionBuilder<T> builder)
            : base(builder)
        {
        }

        // This method needs to be overriden to build our own component the way we want.
        public override ComponentBuilder GetOrAddComponents(bool disableAll, ComponentBuilder builder = null)
        {
            builder ??= new ComponentBuilder();
            foreach (var option in Options)
            {
                var emote = EmoteConverter?.Invoke(option);
                string label = StringConverter?.Invoke(option);
                if (emote is null && label is null)
                {
                    throw new InvalidOperationException($"Neither {nameof(EmoteConverter)} nor {nameof(StringConverter)} returned a valid emote or string.");
                }

                var button = new ButtonBuilder()
                    .WithCustomId(emote?.ToString() ?? label)
                    .WithStyle(option.Style) // Use the style of the option
                    .WithEmote(emote)
                    .WithDisabled(disableAll);

                if (label is not null)
                    button.Label = option.Option.ToString();

                builder.WithButton(button);
            }

            return builder;
        }
    }

    public record ButtonOption<T>(T Option, ButtonStyle Style); // An option with an style
}