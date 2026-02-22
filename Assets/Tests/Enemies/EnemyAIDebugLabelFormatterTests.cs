using Enemies.Debug;
using NUnit.Framework;
using Player.StateMachine;

public class EnemyAIDebugLabelFormatterTests
{
    [Test]
    public void Format_Includes_State_And_AttackPhase()
    {
        var snapshot = new EnemyAIDebugSnapshot("EnemyDefenseTurnState", AttackPhase.Windup, null, null);

        string formatted = EnemyAIDebugLabelFormatter.Format(snapshot);

        Assert.That(formatted, Does.Contain("State: EnemyDefenseTurnState"));
        Assert.That(formatted, Does.Contain("Attack Phase: Windup"));
    }

    [Test]
    public void Format_Includes_Parry_Line_When_Value_Is_Present()
    {
        var snapshot = new EnemyAIDebugSnapshot("EnemyDefenseTurnState", AttackPhase.Recovery, 3, null);

        string formatted = EnemyAIDebugLabelFormatter.Format(snapshot);

        Assert.That(formatted, Does.Contain("Parries Before Counter: 3"));
    }

    [Test]
    public void Format_Includes_Combo_Line_When_Value_Is_Present()
    {
        var snapshot = new EnemyAIDebugSnapshot("EnemyAttackTurnState", AttackPhase.Slash, null, 4);

        string formatted = EnemyAIDebugLabelFormatter.Format(snapshot);

        Assert.That(formatted, Does.Contain("Planned Combo Attacks: 4"));
    }

    [Test]
    public void Format_Omits_Optional_Lines_When_Values_Are_Missing()
    {
        var snapshot = new EnemyAIDebugSnapshot("EnemyIdleState", AttackPhase.Recovery, null, null);

        string formatted = EnemyAIDebugLabelFormatter.Format(snapshot);

        Assert.That(formatted, Does.Not.Contain("Parries Before Counter:"));
        Assert.That(formatted, Does.Not.Contain("Planned Combo Attacks:"));
    }

    [Test]
    public void Format_Output_Is_Stable_And_Ordered()
    {
        var snapshot = new EnemyAIDebugSnapshot("EnemyAttackTurnState", AttackPhase.Slash, 2, 5);

        string formatted = EnemyAIDebugLabelFormatter.Format(snapshot);

        const string expected = "State: EnemyAttackTurnState\n" +
                                "Attack Phase: Slash\n" +
                                "Parries Before Counter: 2\n" +
                                "Planned Combo Attacks: 5";

        Assert.AreEqual(expected, formatted);
    }
}
