using NUnit.Framework;
using BoxingCore;

public class HpEvalTests
{
    [Test] public void Full_health_is_10()
    {
        Assert.AreEqual(10, HpEval.Evaluate(160, 160, 150, 150));
    }
    [Test] public void Head_pinch_only_is_1()
    {
        Assert.AreEqual(1, HpEval.Evaluate(30, 160, 150, 150)); // 頭18.7%≤25%, 腹満タン
    }
    [Test] public void Both_pinch_is_3()
    {
        Assert.AreEqual(3, HpEval.Evaluate(30, 160, 30, 150));
    }
    [Test] public void Body_half_only_is_5()
    {
        Assert.AreEqual(5, HpEval.Evaluate(160, 160, 70, 150)); // 腹46.6%≤50%
    }
    [Test] public void Head_75_only_is_7()
    {
        Assert.AreEqual(7, HpEval.Evaluate(110, 160, 150, 150)); // 頭68.7%≤75%
    }
}
