using InsuranceIntegration.Api.Services.Billing;
using Microsoft.Extensions.Time.Testing;

namespace InsuranceIntegration.Api.Tests.Billing;

public sealed class RefundProcessingServiceTests
{
    private static readonly DateOnly EffectiveDate = new(2026, 3, 1);

    private static RefundProcessingService CreateService() =>
        new(new FakeTimeProvider(new DateTimeOffset(2026, 6, 13, 0, 0, 0, TimeSpan.Zero)));

    private static RefundRequest BuildRequest(
        decimal returnPremium = 300m,
        decimal collectedPremium = 1_200m,
        decimal previouslyRefunded = 0m,
        decimal adminFee = 0m,
        decimal outstandingPremium = 0m,
        string transactionType = RefundTransactionType.Cancellation,
        string payoutMethod = RefundPayoutMethod.BankTransfer,
        string transactionReference = "CAN-1001",
        DateOnly? effectiveDate = null) =>
        new()
        {
            PolicyReference = "POL-REF-001",
            TransactionType = transactionType,
            TransactionReference = transactionReference,
            ReturnPremium = returnPremium,
            CollectedPremium = collectedPremium,
            PreviouslyRefunded = previouslyRefunded,
            AdminFeeWithheld = adminFee,
            OutstandingPremium = outstandingPremium,
            CurrencyCode = "CHF",
            PayoutMethod = payoutMethod,
            EffectiveDate = effectiveDate ?? EffectiveDate
        };

    [Test]
    public void Process_PaysFullReturnPremium_WhenNothingIsWithheld()
    {
        var result = CreateService().Process(BuildRequest());

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(RefundOutcome.Payable));
            Assert.That(result.NetRefundAmount, Is.EqualTo(300m));
            Assert.That(result.RefundableBase, Is.EqualTo(300m));
            Assert.That(result.UncollectedPortion, Is.EqualTo(0m));
            Assert.That(result.CappedByCollectedPremium, Is.False);
            Assert.That(result.RefundReference, Is.EqualTo("RFD-CAN-CAN-1001"));
            Assert.That(result.CurrencyCode, Is.EqualTo("CHF"));
        });
    }

    [Test]
    public void Process_CapsRefundAtPremiumCollected_AndReportsTheUncollectedPart()
    {
        // Half the annual premium was received, so only 600 of the 900 requested can come back.
        var result = CreateService().Process(BuildRequest(returnPremium: 900m, collectedPremium: 600m));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(RefundOutcome.Payable));
            Assert.That(result.RefundableBase, Is.EqualTo(600m));
            Assert.That(result.UncollectedPortion, Is.EqualTo(300m));
            Assert.That(result.NetRefundAmount, Is.EqualTo(600m));
            Assert.That(result.CappedByCollectedPremium, Is.True);
        });
    }

    [Test]
    public void Process_SubtractsPreviouslyRefundedPremiumFromTheCap()
    {
        var result = CreateService().Process(
            BuildRequest(returnPremium: 800m, collectedPremium: 1_000m, previouslyRefunded: 350m));

        Assert.Multiple(() =>
        {
            Assert.That(result.RefundableBase, Is.EqualTo(650m));
            Assert.That(result.NetRefundAmount, Is.EqualTo(650m));
            Assert.That(result.Outcome, Is.EqualTo(RefundOutcome.Payable));
        });
    }

    [Test]
    public void Process_WithholdsTheAdminFee_WithoutLettingItExceedTheRefund()
    {
        var result = CreateService().Process(BuildRequest(returnPremium: 100m, adminFee: 250m));

        Assert.Multiple(() =>
        {
            Assert.That(result.AdminFeeWithheld, Is.EqualTo(100m));
            Assert.That(result.NetRefundAmount, Is.EqualTo(0m));
            Assert.That(result.Outcome, Is.EqualTo(RefundOutcome.NoRefund));
        });
    }

    [Test]
    public void Process_RecoversArrearsBeforePayingTheInsured()
    {
        var result = CreateService().Process(BuildRequest(returnPremium: 500m, outstandingPremium: 180m));

        Assert.Multiple(() =>
        {
            Assert.That(result.OffsetAgainstArrears, Is.EqualTo(180m));
            Assert.That(result.NetRefundAmount, Is.EqualTo(320m));
            Assert.That(result.Outcome, Is.EqualTo(RefundOutcome.Payable));
            Assert.That(result.Reasons, Has.Some.Contains("Offset against outstanding premium: 180"));
        });
    }

    [Test]
    public void Process_ReportsNoRefund_WhenArrearsAbsorbTheWholeReturnPremium()
    {
        var result = CreateService().Process(BuildRequest(returnPremium: 200m, outstandingPremium: 200m));

        Assert.Multiple(() =>
        {
            Assert.That(result.NetRefundAmount, Is.EqualTo(0m));
            Assert.That(result.Outcome, Is.EqualTo(RefundOutcome.NoRefund));
            Assert.That(result.OffsetAgainstArrears, Is.EqualTo(200m));
        });
    }

    [Test]
    public void Process_ReportsExhausted_WhenEveryCollectedUnitWasAlreadyRefunded()
    {
        var result = CreateService().Process(
            BuildRequest(returnPremium: 400m, collectedPremium: 900m, previouslyRefunded: 900m));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(RefundOutcome.Exhausted));
            Assert.That(result.RefundableBase, Is.EqualTo(0m));
            Assert.That(result.NetRefundAmount, Is.EqualTo(0m));
            Assert.That(result.CappedByCollectedPremium, Is.True);
        });
    }

    [Test]
    public void Process_ReportsNoRefund_ForAZeroReturnPremium()
    {
        var result = CreateService().Process(BuildRequest(returnPremium: 0m));

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(RefundOutcome.NoRefund));
            Assert.That(result.NetRefundAmount, Is.EqualTo(0m));
            Assert.That(result.CappedByCollectedPremium, Is.False);
        });
    }

    [Test]
    public void Process_DerivesThePayableDateFromThePayoutChannel()
    {
        var service = CreateService();

        var bank = service.Process(BuildRequest(payoutMethod: RefundPayoutMethod.BankTransfer));
        var card = service.Process(BuildRequest(payoutMethod: RefundPayoutMethod.Card));
        var credit = service.Process(BuildRequest(payoutMethod: RefundPayoutMethod.Credit));

        Assert.Multiple(() =>
        {
            Assert.That(bank.PayableDate, Is.EqualTo(EffectiveDate.AddDays(5)));
            Assert.That(card.PayableDate, Is.EqualTo(EffectiveDate.AddDays(3)));
            Assert.That(credit.PayableDate, Is.EqualTo(EffectiveDate));
        });
    }

    [Test]
    public void Process_BuildsAnEndorsementReference_AndRoundsToTwoDecimals()
    {
        var result = CreateService().Process(BuildRequest(
            transactionType: RefundTransactionType.Endorsement,
            transactionReference: "END-77",
            returnPremium: 123.456m,
            collectedPremium: 2_000m));

        Assert.Multiple(() =>
        {
            Assert.That(result.RefundReference, Is.EqualTo("RFD-END-END-77"));
            Assert.That(result.NetRefundAmount, Is.EqualTo(123.46m));
        });
    }

    [Test]
    public void Process_IsDeterministic_SoAReplayedAdjustmentCannotPayTwice()
    {
        var service = CreateService();
        var request = BuildRequest();

        var first = service.Process(request);
        var second = service.Process(request);

        Assert.Multiple(() =>
        {
            Assert.That(second.RefundReference, Is.EqualTo(first.RefundReference));
            Assert.That(second.NetRefundAmount, Is.EqualTo(first.NetRefundAmount));
            Assert.That(second.PayableDate, Is.EqualTo(first.PayableDate));
            Assert.That(second.Outcome, Is.EqualTo(first.Outcome));
        });
    }

    [Test]
    public void Process_DefaultsTheEffectiveDateToToday()
    {
        var request = new RefundRequest
        {
            PolicyReference = "POL-REF-002",
            TransactionType = RefundTransactionType.Cancellation,
            TransactionReference = "CAN-2002",
            ReturnPremium = 50m,
            CollectedPremium = 50m,
            PayoutMethod = RefundPayoutMethod.Credit
        };

        var result = CreateService().Process(request);

        // FakeTimeProvider is pinned to 2026-06-13 and Credit clears same-day.
        Assert.That(result.PayableDate, Is.EqualTo(new DateOnly(2026, 6, 13)));
    }

    [TestCase("", "CAN-1", RefundTransactionType.Cancellation, RefundPayoutMethod.BankTransfer, TestName = "Process_RejectsAnEmptyPolicyReference")]
    [TestCase("POL-1", "", RefundTransactionType.Cancellation, RefundPayoutMethod.BankTransfer, TestName = "Process_RejectsAnEmptyTransactionReference")]
    [TestCase("POL-1", "CAN-1", "Refund", RefundPayoutMethod.BankTransfer, TestName = "Process_RejectsAnUnknownTransactionType")]
    [TestCase("POL-1", "CAN-1", RefundTransactionType.Cancellation, "Cheque", TestName = "Process_RejectsAnUnknownPayoutMethod")]
    public void Process_RejectsInvalidIdentifiers(string policyReference, string transactionReference, string transactionType, string payoutMethod)
    {
        var request = new RefundRequest
        {
            PolicyReference = policyReference,
            TransactionType = transactionType,
            TransactionReference = transactionReference,
            ReturnPremium = 10m,
            CollectedPremium = 10m,
            PayoutMethod = payoutMethod
        };

        Assert.That(() => CreateService().Process(request), Throws.ArgumentException);
    }

    [TestCase(-1, 0, 0, 0, 0, TestName = "Process_RejectsANegativeReturnPremium")]
    [TestCase(1, -1, 0, 0, 0, TestName = "Process_RejectsNegativeCollectedPremium")]
    [TestCase(1, 10, -1, 0, 0, TestName = "Process_RejectsNegativePreviouslyRefunded")]
    [TestCase(1, 10, 0, -1, 0, TestName = "Process_RejectsANegativeAdminFee")]
    [TestCase(1, 10, 0, 0, -1, TestName = "Process_RejectsNegativeOutstandingPremium")]
    public void Process_RejectsNegativeAmounts(decimal returnPremium, decimal collected, decimal previouslyRefunded, decimal adminFee, decimal outstanding)
    {
        var request = new RefundRequest
        {
            PolicyReference = "POL-1",
            TransactionType = RefundTransactionType.Cancellation,
            TransactionReference = "CAN-1",
            ReturnPremium = returnPremium,
            CollectedPremium = collected,
            PreviouslyRefunded = previouslyRefunded,
            AdminFeeWithheld = adminFee,
            OutstandingPremium = outstanding
        };

        Assert.That(() => CreateService().Process(request), Throws.ArgumentException);
    }
}
