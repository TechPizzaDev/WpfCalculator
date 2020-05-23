using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace WpfCalculator
{
    public class InverseEffect : ShaderEffect
    {
        private static PixelShader _pixelShader = new PixelShader() 
        {
            UriSource = ResourceHelper.MakePackUri("/Content/Shaders/InverseEffect.fxc")
        };

        public static readonly DependencyProperty InputProperty =
            RegisterPixelShaderSamplerProperty(nameof(Input), typeof(InverseEffect), 0);

        public static readonly DependencyProperty ColorMultiplierProperty = DependencyProperty.Register(
            nameof(ColorMultiplier), typeof(Color), typeof(InverseEffect),
            new UIPropertyMetadata(Colors.White, PixelShaderConstantCallback(1)));

        public InverseEffect()
        {
            PixelShader = _pixelShader;
            
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(ColorMultiplierProperty);
        }

        public Brush Input
        {
            get => (Brush)GetValue(InputProperty);
            set => SetValue(InputProperty, value);
        }

        public Color ColorMultiplier
        {
            get => (Color)GetValue(ColorMultiplierProperty);
            set => SetValue(ColorMultiplierProperty, value);
        }

        protected override bool FreezeCore(bool isChecking)
        {
            if (isChecking)
                return false;
            return base.FreezeCore(isChecking);
        }
    }
}