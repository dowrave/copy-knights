namespace System.Runtime.CompilerServices
{
    // [CallerArgumentExpression] 이라는 어트리뷰트를 만들어줌
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }
        public string ParameterName { get; }
    }
}