# Merchant order: framework walkthrough

Status: design specimen for discussion. XML element names, handlers, templates, and test syntax below are proposed vocabulary. There is no implemented loader, schema, or scenario runner for this specimen yet. Numbers are test fixtures, not balance decisions.

The agreed rules in [the main design](../opportunity-framework.md) take precedence. This example checks how those rules fit together without implementing gameplay.

## Example fixture

| Fact | Fixture value |
| --- | --- |
| Estate | Player-owned grain-producing village estate. |
| Buyer | Eligible nearby town with low grain availability and 10,000 available trade funds. |
| Offered goods | 80 grain; outside sourcing is permitted. |
| Negotiated terms for the success test | 12 gold per grain; total 960; hired shipment selected. |
| Response window | Ends on campaign day 1. |
| Buyer deadline | Campaign day 6, derived from fixture buyer urgency. |
| Ordinary village-sale comparison | The same 80 grain would fetch an estimated total of 640. |
| Transport quote | 100, within the enabled transport policy and allowance. |
| Starting estate budget | 500; unrelated income and expenses are zero in this fixture. |
| Starting ordinary stock | 40 grain; shared storage has sufficient room. |
| Staffing | Appointed steward with reservation/diversion authority and sufficient order capacity. |
| Contract policy | Reserve available stock and divert future production into this order. |
| Later production | Another 40 grain becomes available before dispatch. |

Expected additional return for the accepted quote is 960 - 100 - 640 = 220, before any other explicitly introduced costs. The fixture has none. The calculation is performed inside the registered C# rule, not an XML arithmetic expression.

The main example uses one commodity. The definition family also supports bundles; each good must be produced by the source village and has item-level tracking and unit prices, plus any stated full-bundle bonus.

## Responsibility and state

- The opportunity instance owns graph progress, the definition snapshot, stage activations, deadlines, choices, and references to operational records.
- The order record owns quoted/accepted terms, item quantities, reserve references, delivered progress, and settlement status.
- The commitment module owns the buyer payment reservation, estate spending reservations, goods protection, and transport/storage/actor claims.
- The shipment record owns cargo, party and route references, and delivery/return progress.
- The estate economy continues to produce normally and applies authorized diversion before automatic sales.
- Conditions read current facts independently through read-only adapters. Saved events contain type, identities, and campaign time; they do not contain historical cargo snapshots.

## Lifecycle sketch

| Work | Shared lifecycle | Principal result |
| --- | --- | --- |
| Method selection, quote, and negotiation | Offered | Choose a provisional fulfillment method and negotiate its terms; those remain unaccepted until a route confirms acceptance. |
| Acceptance | Offered to active | Revalidate; reserve buyer funds and commit agreed terms. |
| Reservation and delivery | Active | Stock, production, and any authorized purchases fulfill item needs; the chosen method governs movement. |
| Deadline or disruption handling | Active | Authored renegotiation, partial settlement, replacement, or cancellation; existing commitments retain owners. |
| Buyer receipt and payment | Main outcome resolved | Full delivery and payment release the order slot; start cooldown and release generation key. |
| Outstanding returns and cleanup | Settling if needed | Actual actor, cargo, and transport commitments remain owned. |
| Safe completion of all remaining work | Closed | Keep compact history; release unused runtime state and definition snapshot. |

Monitoring a deadline is not an alternative successful delivery. A deadline event takes the missed-deadline route; it must not accidentally satisfy an any-success join. The deadline transition becomes eligible only after counting valid on-time events and finding the obligation still unmet; transition priority does not override the agreed inclusive-deadline rule.

## Proposed XML outline

This outline shows reusable template calls and result wiring. Template implementations still need separate definitions, typed input/output schemas, explicit commitments, failure recovery, and expansion validation. It is not a complete executable definition.

```xml
<Opportunity id="ae:merchant-order" version="1" initialLifecycle="offered">
  <Candidates handler="ae:merchant-order-candidates" handlerVersion="1">
    <Parameter name="goodsPolicy" value="source-village-production" />
    <Output name="estate" type="EstateRef" />
    <Output name="buyer" type="SettlementRef" />
    <Output name="order" type="OrderRef" />
  </Candidates>

  <Uniqueness family="ae:merchant-order">
    <ScopeField name="estate" />
    <ScopeField name="buyer" />
  </Uniqueness>

  <Graph entry="select-method">
    <UseTemplate id="select-method" template="ae:select-order-method" version="1">
      <Bind parameter="order" field="order" />
      <Parameter name="fulfillmentMethods" values="collection,personal-delivery,hired-shipment" />
      <On result="selected" next="negotiate" />
    </UseTemplate>

    <UseTemplate id="negotiate" template="ae:negotiate-order" version="1">
      <Bind parameter="order" field="order" />
      <On result="terms-agreed" next="accept" />
      <On result="declined" next="declined" />
      <On result="response-expired" next="expired" />
    </UseTemplate>

    <UseTemplate id="accept" template="ae:accept-order" version="1">
      <Bind parameter="order" field="order" />
      <Parameter name="executionRoutes" values="personal,steward" />
      <On result="accepted" next="fulfill" />
      <On result="requote-needed" next="negotiate" repeat="bounded" />
    </UseTemplate>

    <Parallel id="fulfill" join="all-required" unfinished="cancel-with-cleanup">
      <Branch id="delivery" required="true">
        <UseTemplate id="deliver" template="ae:fulfill-order" version="1">
          <Bind parameter="order" field="order" />
        </UseTemplate>
      </Branch>
      <Branch id="monitor" required="false">
        <UseTemplate id="watch" template="ae:watch-order" version="1">
          <Bind parameter="order" field="order" />
        </UseTemplate>
      </Branch>
      <On result="fulfilled-and-paid" next="resolve-success" priority="10" />
      <On result="deadline-reached" next="renegotiate" priority="100" />
      <On result="disrupted" next="recover" priority="200" />
    </Parallel>

    <UseTemplate id="renegotiate" template="ae:renegotiate-order" version="1">
      <Bind parameter="order" field="order" />
      <On result="amended" next="fulfill" repeat="bounded" />
      <On result="partial-settlement" next="resolve-partial" />
      <On result="cancelled" next="resolve-cancelled" />
    </UseTemplate>

    <UseTemplate id="recover" template="ae:recover-order" version="1">
      <Bind parameter="order" field="order" />
      <On result="resumed" next="fulfill" repeat="bounded" />
      <On result="settled" next="resolve-recovery" />
    </UseTemplate>

    <!-- Resolution nodes declare reporting outcomes, release the main
         generation key, and enter settlement while obligations remain.
         Decline/expiry nodes and settlement wiring are omitted here. -->
  </Graph>
</Opportunity>
```

The execution route identifies who acts. The fulfillment method identifies how goods reach the buyer. These are different inputs: a steward can accept a hired-shipment order, for example. No companion negotiation route is inferred; an optional shipment leader is assigned through shipment rules.

The uniqueness key is source estate plus buyer, regardless of goods or quantity. This allows one live offer/order for that pair. The outline still leaves exact retry bounds, transition-priority convention, template schemas, and terminal-node syntax for further design. Its merchant values are not embedded into the reusable definition; candidate generation and negotiation produce the order record.

Rejected confirmation is a command-validation result, not a graph transition. It refreshes the same stage without consuming an activation retry, negotiation attempt, resources, or an outcome roll. A successful effect's requote result is distinct from a confirmation that never committed.

Changing the provisional method obtains new method-specific terms without resetting negotiation attempts or buyer patience. The estate may receive a bounded pool of unaccepted offers even when accepted-order capacity is full; response timers keep running and acceptance must still validate available capacity.

Shared trade reputation controls that offer-pool limit. Steward skill separately controls accepted orders. When a steward must choose among acceptable offers, the configured risk approach ranks them: cautious favors reliability; bold favors additional return. Fixture-specific ranking tests will need declared inputs and expected ordering rather than a universal highest-profit rule.

Personal acceptance may fall outside the steward's risk/return preferences if mandatory rules still pass. A specifically approved personal purchase does not consume automation allowance, even when execution happens later. An estate without a steward can execute a previously approved hired dispatch when ready; that does not grant authority for new autonomous buying or resource decisions. The fixture must state whether transport is personally approved or chosen autonomously so allowance assertions use the correct authority.

For an estate without a steward, saved approvals cover dispatch and returns only. The player must source and reserve goods manually; automatic diversion in the main fixture depends on its appointed steward. Approvals use explicit maximum-cost, allowed-route, and minimum-escort bounds. A policy-change review lets the player keep or revoke affected unstarted personal approvals.

Approval records permission only; it does not hold money or transport capacity. Transport booking occurs only when the full order reserve is ready. Recheck funds, available transport, and approval bounds then. The buyer's payment reservation on acceptance is separate from this later estate transport booking.

| Moment in this fixture | Estate money held for transport | Buyer payment held |
| --- | --- | --- |
| Dispatch approval and order acceptance | 0 | 960 |
| Waiting with 40 of 80 grain reserved | 0 | 960 |
| Full goods reserve and successful booking | Quoted 100, until charged at preparation | 960 |
| Preparation paid | 100 spent, with no duplicate held amount | 960 |
| Full receipt and payment | No remaining transport hold | Released as 960 estate income |

## Acceptance and normal success

1. Generate the eligible offer and preserve its quoted terms through the response window. No buyer money is reserved yet.
2. Choose a provisional fulfillment method, then commit a negotiation attempt for that method using current read-only queries. Save its hidden outcome and update the quoted terms through journaled instructions. An agreement here is not acceptance of a delivery obligation.
3. Confirm acceptance and the fulfillment method. Recheck authority, funds, capacity, commitments, and forecast. On failure, spend nothing and roll no new outcome.
4. Reserve 960 from the buyer town's trade funds. The accepted order now has guaranteed payment, but the estate has not received it.
5. Reserve the available 40 grain, then divert the next 40 under the enabled policy. The protected reserve reaches 80. This planned production wait does not itself prompt as blocked automation.
6. Execute the specifically approved hired shipment when ready. In this personal-approval fixture, reserve the 100 transport charge from estate funds without consuming automation allowance; charge when preparation begins. An autonomous steward booking would also reserve and consume the relevant allowance. Cargo leaves the reserve for the actual shipment.
7. The visible party reaches the buyer. Actual delivery transfers the 80 grain into the town market and completes delivery progress. Release the 960 payment to the estate budget once the full order is fulfilled.
8. With no other fixture entries, the estate balance is 500 - 100 + 960 = 1,360. Release the active-order slot and generation key, and begin cooldown. Hired capacity remains unavailable for its quoted turnaround delay.
9. Safely finish any remaining owned work, then close and compact history.

## Proposed offline scenario fragment

The fixture and step handlers are proposed test vocabulary, not existing loader features. The arrival step changes the simulated world location; the event contains no cargo-quantity snapshot. The normal runtime must perform fulfillment and payment rather than the test directly setting those results.

```xml
<Scenario id="ae:test-merchant-order-success">
  <Given fixture="ae:fixture-grain-order" />
  <RandomDraws>
    <Draw value="0.25" />
  </RandomDraws>
  <Choose instance="offer-1" choice="select-method" route="personal"
          fulfillmentMethod="hired-shipment" />
  <Choose instance="offer-1" choice="firm-price" route="personal" />
  <Expect scope="order" ref="order-1" field="quotedTotal" equals="960" />
  <Choose instance="offer-1" choice="accept" route="personal" />
  <Expect scope="buyer" ref="buyer-town" field="reservedPayment" equals="960" />
  <Expect scope="order" ref="order-1" field="reservedGrain" equals="40" />
  <Expect scope="estate" ref="estate-1" field="budget" equals="500" />
  <Expect scope="estate" ref="estate-1" field="transportFundsHeld" equals="0" />

  <AdvanceTo campaignDay="2" />
  <WorldAction handler="ae:test-produce-goods" estate="estate-1"
               good="grain" quantity="40" />
  <ProcessPending />
  <Expect scope="order" ref="order-1" field="dispatchedGrain" equals="80" />

  <Save checkpoint="after-dispatch" />
  <Load checkpoint="after-dispatch" />
  <AdvanceTo campaignDay="4" />
  <WorldAction handler="ae:test-arrive-party" party="shipment-party-1"
               settlement="buyer-town" />
  <Event id="arrival-1" type="ae:party-arrived" campaignDay="4">
    <Identity role="party" value="shipment-party-1" />
    <Identity role="settlement" value="buyer-town" />
  </Event>
  <ProcessPending />

  <Expect scope="order" ref="order-1" field="deliveredGrain" equals="80" />
  <Expect scope="order" ref="order-1" field="paidTotal" equals="960" />
  <Expect scope="estate" ref="estate-1" field="budget" equals="1360" />
  <Expect scope="estate" ref="estate-1" field="activeOrderCount" equals="0" />
  <Expect scope="opportunity" ref="offer-1" field="mainOutcome" equals="success" />
</Scenario>
```

The scripted draw and proposed syntax are illustrative, not a claim that an implemented probability table currently produces these terms. The future fixture must declare the roll's input range and outcome weights so the scripted draw exercises the intended result through the normal runtime calculation. Save/load restores the script cursor and committed results; it does not replay an already consumed negotiation roll. The scenario fails if it requests an unavailable draw or finishes with unused draws, reporting the mismatched consumption location.

## Additional scenario coverage

Late arrival and renegotiation must distinguish location from handover. A late shipment can be at the buyer settlement with its cargo still on the party, zero payment released, and no delivered credit for that cargo. Agreeing revised terms can then permit handover. A collection party waiting after an unmet deadline remains only through the visible renegotiation response window; expiry makes it leave, not take the estate's goods without agreement.

If that response window expires, apply the breach settlement disclosed in the accepted terms. Use actual delivered progress, fees, reputation effects, and reserved buyer funds. Undelivered cargo returns under an existing return approval or enabled policy; missing authority or an unsafe route prompts for a decision while ownership remains tracked. A breach outcome does not imply that the party or cargo can be deleted when its order ends.

Any remaining breach-fee shortfall becomes a saved estate liability. Protect existing booked funds and due upkeep first, then apply remaining income to liabilities before withdrawals. The player can also pay manually from clan gold. The record must retain creditor, source, outstanding amount, and payment identity; retries and reloads must not duplicate either the debt or a payment. New orders remain eligible when their plans account for repayments and pass normal resource, capacity, and authority checks. The independent financial record owns repayment and its source-history link, so the original opportunity can close once its other obligations are safely settled.

| Scenario | Required observation |
| --- | --- |
| Invalid acceptance | Another commitment consumes available funds; acceptance refreshes without spending, rolling, advancing lifecycle, or consuming negotiation attempts or graph retries. |
| Duplicate arrival | Reprocessing the same event does not deliver or pay twice. |
| Deadline boundary | Delivery at day 6 counts before expiry; delivery after the deadline takes the authored late path. |
| Late arrival during negotiation | A party arriving after the deadline waits with its cargo; do not credit delivery, add buyer stock, or pay until revised terms or another agreed response permit handover. |
| Collection response window | An already waiting buyer party stays through the displayed renegotiation window after an unmet deadline, then leaves if no agreement is reached. |
| Renegotiation expiry | Apply the previously disclosed breach settlement once, reconcile buyer funds and delivered goods, and issue only authorized return instructions for undelivered cargo. |
| Fee shortfall | Create one estate liability for the unpaid portion; future-income and manual clan-gold payments reduce it exactly once, before automatic withdrawals. |
| Return cannot proceed | With no valid approval/policy or safe route, prompt and preserve the party and cargo rather than silently spending, teleporting, or discarding them. |
| Personal partial delivery | Deliver 30 and then 50 matching grain from ordinary party inventory; progress is permanent, all payment stays held until full delivery or negotiated settlement. |
| Early collection signal | Signal with only 40 reserved; buyer dispatches, but seller deadline protection begins only when all 80 are in the protected reserve. |
| Interrupted preparation | Save/recover around spending and party creation; do not charge twice, spawn duplicate parties, or lose cargo ownership. |
| Destroyed shipment | The order remains active; use current records and recovery rules without assuming an event saved historical cargo facts. |
| Player-resource return | With player troops/animals, successful delivery releases the order slot while physical return obligations keep recovery/settlement owned. |
| Definition update | A new generation uses the updated definition; this active instance retains its expanded snapshot and compatible handlers. |
| Technical abandonment | Preview recovery/forfeiture, waive technical-failure fees/reputation loss, recall or redirect player parties, and retain settlement until obligations resolve. |
| Feasibility deterioration | Directly warn when a plan leaves permitted risk or is projected late; lesser deterioration updates the normal view without a new direct warning. |
| Merchant uniqueness | Another goods set or quantity cannot create a second live offer/order for the same estate and buyer; another eligible buyer can have a distinct key. |
| Provisional-method switch | Method-specific terms change, but the same negotiation attempt count, buyer patience, and estate–buyer key remain. |
| Offer at accepted-order cap | A different buyer can supply an offer if the offer pool has room. Acceptance fails without a slot, leaving the offer and its original response timer intact. |
| Pool versus order limits | Trade reputation changes the offered-pool limit without changing the Steward-derived accepted-order cap. |
| Risk-based ranking | Given two eligible offers and one free slot, cautious and bold policies can rank them differently without bypassing mandatory eligibility or spending rules. |
| Manual authority | Personal acceptance can knowingly exceed steward risk preferences; a personally approved purchase reduces available estate funds without consuming automation allowance. |
| Previously approved dispatch | With no steward, manually source and reserve the goods; approved dispatch/returns can execute within their bounds without authorizing automatic buying, reservation, or production diversion. |
| Policy-change review | Show affected unstarted personal approvals and preserve or revoke them according to the player's explicit selection; do not undo already executed work. |
| Approval without booking | With an incomplete goods reserve, approval holds no estate funds or transport capacity. Booking begins only after all required goods are reserved. |
| Liability repayment priority | Protect existing booked funds and due upkeep, repay debt from remaining income, and withdraw only after repayment priorities are satisfied. |
| Contracting while indebted | Allow a new order whose plan accounts for repayment and passes normal checks; reject an otherwise unaffordable plan with a clear reason. |
| Independent debt ownership | Close an otherwise settled source opportunity while its financial record continues repayment; preserve source history and prevent duplicate payments after reload. |
| Final-plot sale with debt | Preview sale proceeds applied to liabilities and the net return. If a shortfall remains, require payment or explicit debt transfer before completing the sale; preserve the estate and debt if the sale cannot complete. |
| Liability transfer | Allow any other player-owned estate after previewing its existing debts and projected repayment; move responsibility without duplicating principal or losing payment and source history. |
| Event recipient boundary | A delivery may credit explicitly matching objectives already active. A stage opened by that delivery must use saved progress or await a new event rather than receiving an automatic replay. |
| Immediate progression | Once saved delivery progress satisfies the order, continue eligible settlement stages until a decision, timed wait, new event, or unfinished effect is required. Enforce a safety limit without replaying the arrival event or repeating payment. |
| Overlapping pauses | Pause one deadline for two independent reasons; clearing one keeps it paused. Clear the last reason to resume, counting the overlapping interval once, including across save/load. |
| Progress during clock pause | While war pauses the delivery deadline, allow otherwise valid reservation and renegotiation actions and process progress events; continue enforcing access, authority, and resource rules. |

## Questions exposed by the specimen

1. When may a durable gameplay listener acknowledge an event whose processing may start further journaled work?
