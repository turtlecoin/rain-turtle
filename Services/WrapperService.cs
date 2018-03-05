using System;
using System.ComponentModel;

namespace TurtleBot.Services
{
    public class WrapperService<T> : WrapperService
    {
        public T Value { get; private set; }

        public Func<object, T> Converter { private get; set; }

        public WrapperService()
        {
            _valueConverter = TypeDescriptor.GetConverter(typeof(T));

            Converter = value => (T) _valueConverter.ConvertFrom(value);
        }

        private readonly TypeConverter _valueConverter;


        public object GetValue()
        {
            return Value;
        }

        public void SetValue(object value)
        {

            Value = Converter.Invoke(value);
        }
    }

    public interface WrapperService
    {
        object GetValue();

        void SetValue(object value);
    }
}