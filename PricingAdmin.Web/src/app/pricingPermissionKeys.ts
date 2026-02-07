export const PricingPermissionKeys = {
  QuotesCreate: 'Pricing.Quotes.Create',

  PriceListsView: 'Pricing.PriceLists.View',
  PriceListsDetailView: 'Pricing.PriceLists.Detail.View',
  PriceListsCreate: 'Pricing.PriceLists.Create',
  PriceListsEdit: 'Pricing.PriceLists.Edit',
  PriceListsDelete: 'Pricing.PriceLists.Delete',

  OverridesView: 'Pricing.Overrides.View',
  OverridesDetailView: 'Pricing.Overrides.Detail.View',
  OverridesCreate: 'Pricing.Overrides.Create',
  OverridesEdit: 'Pricing.Overrides.Edit',
  OverridesDelete: 'Pricing.Overrides.Delete',

  CampaignsView: 'Pricing.Campaigns.View',
  CampaignsDetailView: 'Pricing.Campaigns.Detail.View',
  CampaignsCreate: 'Pricing.Campaigns.Create',
  CampaignsEdit: 'Pricing.Campaigns.Edit',
  CampaignsDelete: 'Pricing.Campaigns.Delete',

  CouponsView: 'Pricing.Coupons.View',
  CouponsDetailView: 'Pricing.Coupons.Detail.View',
  CouponsUsageView: 'Pricing.Coupons.Usage.View',
  CouponsReservationsView: 'Pricing.Coupons.Reservations.View',
  CouponsCreate: 'Pricing.Coupons.Create',
  CouponsReserve: 'Pricing.Coupons.Reserve',
  CouponsEdit: 'Pricing.Coupons.Edit',
  CouponsDelete: 'Pricing.Coupons.Delete',

  BulkPricesUpdate: 'Pricing.Bulk.Prices.Update',
  BulkCampaignsCategoryCreate: 'Pricing.Bulk.Campaigns.Category.Create',

  PoliciesListView: 'Pricing.Policies.List.View',
  PoliciesView: 'Pricing.Policies.View',
  PoliciesEdit: 'Pricing.Policies.Edit',
} as const

export type PricingPermissionKey = typeof PricingPermissionKeys[keyof typeof PricingPermissionKeys]
