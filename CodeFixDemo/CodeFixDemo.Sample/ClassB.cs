using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;

namespace CodeFixDemo.Sample;

public class ClassB
{
    public bool SomeBooleanMethod(IEnumerable<int> myEnumerable)
    {
        return myEnumerable.IsNullOrEmpty();
    }
}