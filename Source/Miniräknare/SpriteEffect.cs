using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Miniräknare
{
    public class SpriteEffect : ShaderEffect
    {
        private static PixelShader _pixelShader = new PixelShader() 
        {
            UriSource = ResourceHelper.MakePackUri("/Content/Shaders/SpriteEffect.fxc")
        };

        public static readonly DependencyProperty InputProperty =
            RegisterPixelShaderSamplerProperty(nameof(Input), typeof(SpriteEffect), 0);

        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            nameof(Color), typeof(Color), typeof(SpriteEffect),
            new UIPropertyMetadata(Colors.White, PixelShaderConstantCallback(1)));

        public SpriteEffect()
        {
            PixelShader = _pixelShader;

            UpdateShaderValue(InputProperty);
            UpdateShaderValue(ColorProperty);
        }

        public Brush Input
        {
            get => (Brush)GetValue(InputProperty);
            set => SetValue(InputProperty, value);
        }

        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        protected override bool FreezeCore(bool isChecking)
        {
            if (isChecking)
                return false;
            return base.FreezeCore(isChecking);
        }
    }
}