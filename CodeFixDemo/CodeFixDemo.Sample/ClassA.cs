using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;

namespace CodeFixDemo.Sample;

public class ClassA
{
    public void DoSomething()
    {
        IEnumerable<int> myEnumerable = [1, 2, 3, 4];
        if (myEnumerable.IsNullOrEmpty())
        {
            // code
        }
    }
}