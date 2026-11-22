# DealerSystemSetting – Control / Database Mapping

Bu dosya HTML form kontrollerini `DealerSettings` sütunlarıyla eşleştirmek için hazırlanmış bir aday haritasıdır. ASP.NET code-behind bulunmadığından hiçbir eşleşme kesin kayıt eşlemesi olarak kabul edilmemelidir. Yazma işlemi geliştirilmeden önce API/persistence davranışı doğrulanmalıdır.

## Durum özeti

| Durum | Adet |
|---|---:|
| `semantic_control_mapping_candidate` | 93 |
| `unresolved_control_has_no_clear_column` | 2 |
| `unresolved_no_schema_match` | 3 |

## Kontrol bazında aday eşleştirmeler

### User / General

| Ekran etiketi | HTML kontrolü | DB sütunu adayı | Durum |
|---|---|---|---|
| Site State | `ctl00$PageContent$ddlSiteStatus` | `SiteStatus` | `semantic_control_mapping_candidate` |

### User / Visual Settings

| Ekran etiketi | HTML kontrolü | DB sütunu adayı | Durum |
|---|---|---|---|
| Layout | `ctl00$PageContent$ddlLayout` | `Layout` | `semantic_control_mapping_candidate` |
| Product Image Aspect Ratio | `ctl00$PageContent$txtProductImageAspectRatio` | `ProductImageAspectRatio` | `semantic_control_mapping_candidate` |
| Logo (Dark) | `ctl00$PageContent$imgLogo$imageUpload` | `Logo` | `semantic_control_mapping_candidate` |
| Logo (Light) | `ctl00$PageContent$imgLogo2$imageUpload` | `Logo2` | `semantic_control_mapping_candidate` |
| Logo (Log In) | `ctl00$PageContent$imgLogoLogin$imageUpload` | `LogoLogin` | `semantic_control_mapping_candidate` |
| Favicon | `ctl00$PageContent$imgFavicon$imageUpload` | `Favicon` | `semantic_control_mapping_candidate` |
| Default Image in Non-Image Products | `ctl00$PageContent$imgNoImage$imageUpload` | `NoImageImage` | `semantic_control_mapping_candidate` |

### User / Color Settings

| Ekran etiketi | HTML kontrolü | DB sütunu adayı | Durum |
|---|---|---|---|
| Layout Color | `ctl00$PageContent$txtLayoutColor` | `LayoutColor` | `semantic_control_mapping_candidate` |
| Layout Color 2 | `ctl00$PageContent$txtLayoutColor2` | `LayoutColor2` | `semantic_control_mapping_candidate` |

### User / Google Analytics / Tag Manager Settings

| Ekran etiketi | HTML kontrolü | DB sütunu adayı | Durum |
|---|---|---|---|
| Meta Tags | `ctl00$PageContent$txtGoogleAnalysticScript` | `GoogleAnalysticScript` | `semantic_control_mapping_candidate` |
| Script Codes | `ctl00$PageContent$txtGoogleAnalysticProfileID` | `GoogleAnalysticProfileID` | `semantic_control_mapping_candidate` |
| NoScript Codes | `ctl00$PageContent$txtGoogleAdWordsReMarketingCode` | `GoogleAdWordsReMarketingCode` | `semantic_control_mapping_candidate` |

### Admin / Design Settings

| Ekran etiketi | HTML kontrolü | DB sütunu adayı | Durum |
|---|---|---|---|
| Category Menu | `ctl00$PageContent$ddlCategoryMenu` | `CategoryMenuVisible` | `semantic_control_mapping_candidate` |
| Design Parameters | `ctl00$PageContent$ddlParameterDesign` | `ParameterDesign` | `semantic_control_mapping_candidate` |
| Theme Parameter | `ctl00$PageContent$ddParameterThema` | `ParameterTheme` | `semantic_control_mapping_candidate` |
| Don't Show Menu At Dealers | `ctl00$PageContent$ddlDontShowMenuAtDealers` | `DontShowMenuAtDealers` | `semantic_control_mapping_candidate` |
| Filter Show | `ctl00$PageContent$ddlFilterSort` | `SortFilterShow` | `semantic_control_mapping_candidate` |
| Filter Display | `ctl00$PageContent$ddUseFiltering` | `UseFiltering` | `semantic_control_mapping_candidate` |
| Product Sorting | `ctl00$PageContent$ddlProductSort` | `ProductSort` | `semantic_control_mapping_candidate` |
| Dealer Limit Tracking | `ctl00$PageContent$ddlCariLimitTracing` | `LimitTracing` | `semantic_control_mapping_candidate` |
| Current Limit Warning | `ctl00$PageContent$ddlCariLimitTracingInfo` | `LimitTracingInfo` | `semantic_control_mapping_candidate` |
| Automatic Definition | `ctl00$PageContent$ddlSchedule` | `Schedule` | `semantic_control_mapping_candidate` |

### Admin / Product Display Settings

| Ekran etiketi | HTML kontrolü | DB sütunu adayı | Durum |
|---|---|---|---|
| Dont Show Price | `ctl00$PageContent$ddlDontShowPrice` | `DoNotShowPrice` | `semantic_control_mapping_candidate` |
| Stock Tracking | `ctl00$PageContent$ddlHasStock` | `HasStockVisible` | `semantic_control_mapping_candidate` |
| Stock Presentation Type | `ctl00$PageContent$ddlProductTableStockType` | `ProductTableStockType` | `semantic_control_mapping_candidate` |
| Stock Display Parameter | `ctl00$PageContent$ddlParameterStock` | `ParameterStock` | `semantic_control_mapping_candidate` |
| Tax Excluded Products Show | `ctl00$PageContent$ddlTaxInRatio` | `TaxInRatio` | `semantic_control_mapping_candidate` |
| B2B & B2C Partners | `ctl00$PageContent$ddlCommonSystem` | `ProductCompany` | `semantic_control_mapping_candidate` |
| Product Detail Parameter | `ctl00$PageContent$ddlProductDetailParameter` | `ParameterProductDetail` | `semantic_control_mapping_candidate` |
| Product Detail Parameter 2 | `ctl00$PageContent$ddlProductDetailParameter2` | `ParameterProductDetail2` | `semantic_control_mapping_candidate` |
| Ürün Detay Parametresi (B2C - Oturum Kapalı) | `ctl00$PageContent$ddlParameterProductDetailNotLogin` | `ParameterProductDetailNotLogin` | `semantic_control_mapping_candidate` |
| Ürün Detay Parametresi 2 (B2C - Oturum Kapalı) | `ctl00$PageContent$ddlParameterProductDetailNotLogin2` | — | `unresolved_control_has_no_clear_column` |
| Product Search Parameter | `ctl00$PageContent$ddlProductSearchParameter` | `ParameterProductSearch` | `semantic_control_mapping_candidate` |
| Show Export Button | `ctl00$PageContent$ddlExportProductList` | `ExportProductList` | `semantic_control_mapping_candidate` |
| Parametric Modules | `ctl00$PageContent$ddlParameterModules` | `ParameterModules` | `semantic_control_mapping_candidate` |

### Admin / Cart Settings

| Ekran etiketi | HTML kontrolü | DB sütunu adayı | Durum |
|---|---|---|---|
| Total Minimum Cart | `ctl00$PageContent$txtMinCartCount` | `MinCartCount` | `semantic_control_mapping_candidate` |
| Payment Detail Show | `ctl00$PageContent$ddlPaymentDetailShow` | `PaymentDetailShow` | `semantic_control_mapping_candidate` |
| Show  Product Variation Code In Cart | `ctl00$PageContent$ddlCartProductVariationCodeShow` | `CartProductVariationCodeShow` | `semantic_control_mapping_candidate` |
| Min Cart Price | `ctl00$PageContent$txtMinCartTotal` | `MinCartTotal` | `semantic_control_mapping_candidate` |
| Maximum Product Order Quantity | `ctl00$PageContent$txtMaximumOrderQuantity` | `MaximumOrderQuantity` | `semantic_control_mapping_candidate` |

### Admin / General Settings

| Ekran etiketi | HTML kontrolü | DB sütunu adayı | Durum |
|---|---|---|---|
| Delivery Date Get | `ctl00$PageContent$chkDeliveryDateShow` | `DeliveryDateShow` | `semantic_control_mapping_candidate` |
| Minimum Inventory Quantity | `ctl00$PageContent$txtMinStockFilter` | `MinStockFilter` | `semantic_control_mapping_candidate` |
| Discount Type | `ctl00$PageContent$ddlDiscountType` | `DiscountType` | `semantic_control_mapping_candidate` |
| Login Screen  ( Login Page ) | `ctl00$PageContent$chkCustomerCodeForLogin` | `UseCustomerCodeForLogin` | `semantic_control_mapping_candidate` |
| Bank Installments Impressions | `ctl00$PageContent$ddlBankTableType` | `BankTableType` | `semantic_control_mapping_candidate` |
| Login With Expiry | `ctl00$PageContent$chkLoginWithExpiry` | `LoginWithExpiry` | `semantic_control_mapping_candidate` |
| Use Currency Type | `ctl00$PageContent$ddlCurrencyType` | `UseCurrencyType` | `semantic_control_mapping_candidate` |
| Current Maturity Date | `ctl00$PageContent$ddlExpiryDate` | `ExpiryDate` | `semantic_control_mapping_candidate` |
| Current As Well Total Balance | `ctl00$PageContent$ddlAccountActivityGenralCuurencyTableShow` | `AccountActivityGenralCuurencyTableShow` | `semantic_control_mapping_candidate` |
| E-PayCommission | `ctl00$PageContent$ddlCommissionType` | `CommissionType` | `semantic_control_mapping_candidate` |
| Discount Authorization for Customer Representative | `ctl00$PageContent$ddlSpecialDiscount` | `SpecialDiscount` | `semantic_control_mapping_candidate` |
| Registration Form Link | `ctl00$PageContent$txtRegisterFormUrl` | `RegisterFormUrl` | `semantic_control_mapping_candidate` |
| Main Page | `ctl00$PageContent$ddlHomeStyle` | `HomeStyle` | `semantic_control_mapping_candidate` |
| Current Account Payment | `ctl00$PageContent$ddlPaymentForAccountActivityShow` | `PaymentForAccountActivityShow` | `semantic_control_mapping_candidate` |
| Dealer Phone | `ctl00$PageContent$txtBayiTelefon` | `DealerPhoneNumber` | `semantic_control_mapping_candidate` |
| Dealer Adress | `ctl00$PageContent$txtBayiAdres` | `DealerAdress` | `semantic_control_mapping_candidate` |
| Search By Brand | `ctl00$PageContent$ddlSearchMarka` | `SearchShowBrand` | `semantic_control_mapping_candidate` |
| Advanced Search | `ctl00$PageContent$ddlSearchDetail` | `SearchShowDetail` | `semantic_control_mapping_candidate` |
| Using the Quick Search Listings | `ctl00$PageContent$chkDontUseLcnAutoComplate` | `DontUseLcnAutoComplate` | `semantic_control_mapping_candidate` |
| Desktop Application | `ctl00$PageContent$ddlUseDesktopApi` | `UseDesktopApi` | `semantic_control_mapping_candidate` |
| Current Account Details | `ctl00$PageContent$ddlRowDescriptionShow` | `RowDescriptionShow` | `semantic_control_mapping_candidate` |
| Product Currency | `ctl00$PageContent$ddlProductCurrency` | `CurrencyProduct` | `semantic_control_mapping_candidate` |
| Pos Currency | `ctl00$PageContent$ddlPosCurrency` | `CurrencyPos` | `semantic_control_mapping_candidate` |
| Integration     Parameter | `ctl00$PageContent$ddlParameterIntegration` | `ParameterIntegration` | `semantic_control_mapping_candidate` |
| Show details add button is clicked | `ctl00$PageContent$chkAddCartAction` | `AddCartAction` | `semantic_control_mapping_candidate` |
| Do Not Send Mail To Customer | `ctl00$PageContent$chkDoNotSendMailToCustomer` | `DoNotSendMailToCustomer` | `semantic_control_mapping_candidate` |
| Send Delivery Address If Different | `ctl00$PageContent$ddlSendDeliveryAddressIfDifferent` | `SendDeliveryAddressIfDifferent` | `semantic_control_mapping_candidate` |
| Currency Price Type | `ctl00$PageContent$ddlCurrencyPriceType` | `CurrencyPriceType` | `semantic_control_mapping_candidate` |
| All Products | `ctl00$PageContent$ddlAllProductsShow` | `AllProductsShow` | `semantic_control_mapping_candidate` |
| Different Payment Link | `ctl00$PageContent$txtPaymentLink` | `PaymentLink` | `semantic_control_mapping_candidate` |
| Payment Parameters | `ctl00$PageContent$ddlPaymentParameter` | `ParameterPayment` | `semantic_control_mapping_candidate` |

### Admin / Application Form Settings

| Ekran etiketi | HTML kontrolü | DB sütunu adayı | Durum |
|---|---|---|---|
| Application Form Settings | `ctl00$PageContent$ddlParameterLogin` | `ParameterLogin` | `semantic_control_mapping_candidate` |
| Application Form Menu Display | `ctl00$PageContent$ddlParameterLoginMenu` | `ParameterLoginMenu` | `semantic_control_mapping_candidate` |

### Admin / Admin

| Ekran etiketi | HTML kontrolü | DB sütunu adayı | Durum |
|---|---|---|---|
| Admin  Parameter | `ctl00$PageContent$ddlParameterAdmin` | `ParameterAdmin` | `semantic_control_mapping_candidate` |
| Cart Parameter | `ctl00$PageContent$ddlOrderParameter` | `ParameterOrder` | `semantic_control_mapping_candidate` |
| Account Parameter | `ctl00$PageContent$ddlParameterAccount` | `ParameterAccount` | `semantic_control_mapping_candidate` |
| Main Products | `ctl00$PageContent$ddMainProducts` | `MainProducts` | `semantic_control_mapping_candidate` |
| Integrated | `ctl00$PageContent$ddlintegrated` | `Integrated` | `semantic_control_mapping_candidate` |
| Discount Type | `ctl00$PageContent$ddldiscountTypeInvoice` | `DiscountTypeInvoice` | `semantic_control_mapping_candidate` |
| Add New Shipping Address | `ctl00$PageContent$DDLDeliveryAddress` | `DeliveryAddress` | `semantic_control_mapping_candidate` |
| Bonus | `ctl00$PageContent$ddlBonusShow` | `BonusShow` | `semantic_control_mapping_candidate` |
| Site Map | `ctl00$PageContent$ddlSiteMap` | `SiteMap` | `semantic_control_mapping_candidate` |
| Dealer Sub-Dealer Property | `ctl00$PageContent$ddSubDealerShow` | `SubDealerShow` | `semantic_control_mapping_candidate` |
| Adding Address You Added Company Information | `ctl00$PageContent$chkBillingAdress` | `BillingAdressControl` | `semantic_control_mapping_candidate` |
| Dealer Name | `ctl00$PageContent$TxtDealerName` | `DealerName` | `semantic_control_mapping_candidate` |
| Recaptcha Code | `ctl00$PageContent$txtBayiYetkiliKisi` | `DealerAutHorizePersonel` | `semantic_control_mapping_candidate` |
| Dealer Website | `ctl00$PageContent$txtBayiWebAdres` | `DealerWebAdress` | `semantic_control_mapping_candidate` |
| E Invoice No | `ctl00$PageContent$txtEInvoiceNo` | `EInvoiceNo` | `semantic_control_mapping_candidate` |
| Use Encryption | `ctl00$PageContent$ddlUseMd5Encryption` | `UseMd5Encryption` | `semantic_control_mapping_candidate` |
| Variations Showing | `ctl00$PageContent$ddlVariationShow` | `VariationShow` | `semantic_control_mapping_candidate` |
| Menu Display | `ctl00$PageContent$ddlParameterMenu` | `ParameterMenu` | `semantic_control_mapping_candidate` |
| Menu Display 2 | `ctl00$PageContent$ddlParameterMenu2` | — | `unresolved_control_has_no_clear_column` |
| Dynamic Search Parameter | `ctl00$PageContent$ddlParameterDynamicSearch` | `ParameterDynamicSearch` | `semantic_control_mapping_candidate` |
| Currency type in order integration transfer | `ctl00$PageContent$ddIntegrationCurrency` | `IntegrationCurrencyID` | `semantic_control_mapping_candidate` |
| Send WhatsApp Order Notification | `ctl00$PageContent$chkWhatsAppOrderNotification` | — | `unresolved_no_schema_match` |
| Favorite Icon Position / Left / Right / Top / Bottom | `ctl00$PageContent$FavoriteIconPosition` | — | `unresolved_no_schema_match` |
| Favorite Icon Position / Left / Right / Top / Bottom | `ctl00$PageContent$FavoriteIconVerticalPosition` | — | `unresolved_no_schema_match` |

## Güvenli uygulama kuralları

- `Parameter*` alanları bit maskesidir: bir seçeneği açıp kapatırken diğer bitler korunmalıdır.
- SQL `NULL` ile `0` ayrımı korunmalıdır; otomatik `NULL` → `0` dönüşümü yapılmamalıdır.
- `MemberCode` kapsamı/tenant seçimi açıkça belirlenmeden kayıt güncellenmemelidir.
- Ekrandaki mevcut seçili seçenekler `store1` oturumuna aittir; yerel `store5.bak` verileriyle karıştırılmamalıdır.
- Kayıt eşleştirmeleri code-behind, gerçek endpoint veya test amaçlı izole veritabanında doğrulanmadan yazma kodu üretilmemelidir.
