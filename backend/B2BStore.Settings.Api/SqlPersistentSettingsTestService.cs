using System.Data;
using System.Globalization;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace B2BStore.Settings.Api;

/// <summary>Fixed, local-only committed test path. No client-supplied identifiers are used.</summary>
public sealed class SqlPersistentSettingsTestService(
    ParameterCatalog catalog,
    string? connectionString,
    string? memberCode,
    bool testModeEnabled,
    bool hostedMode = false)
{
    private const string SiteStatusId = "ctl00_PageContent_ddlSiteStatus";
    private const string ParameterOrderId = "ctl00_PageContent_ddlOrderParameter";
    private const string LayoutId = "ctl00_PageContent_ddlLayout";
    private const string MenuVisibilityId = "ctl00_PageContent_ddlDontShowMenuAtDealers";
    private const string DeliveryDateId = "ctl00_PageContent_chkDeliveryDateShow";
    private const string ScheduleId = "ctl00_PageContent_ddlSchedule";
    private static readonly string[] AllowedColumns = ["SiteStatus", "ParameterOrder", "Layout", "DontShowMenuAtDealers", "DeliveryDateShow"];
    private static readonly IReadOnlySet<string> NumericTextScalarIds = new HashSet<string>(StringComparer.Ordinal)
    {
        "ctl00_PageContent_txtMinCartCount",
        "ctl00_PageContent_txtMinCartTotal",
        "ctl00_PageContent_txtMaximumOrderQuantity",
    };
    private static readonly IReadOnlySet<string> TextScalarIds = new HashSet<string>(StringComparer.Ordinal)
    {
        "ctl00_PageContent_txtProductImageAspectRatio", "ctl00_PageContent_txtLayoutColor", "ctl00_PageContent_txtLayoutColor2",
        "ctl00_PageContent_txtGoogleAnalysticScript", "ctl00_PageContent_txtGoogleAnalysticProfileID",
        "ctl00_PageContent_txtGoogleAdWordsReMarketingCode", "ctl00_PageContent_txtMinStockFilter",
        "ctl00_PageContent_txtRegisterFormUrl", "ctl00_PageContent_txtBayiTelefon", "ctl00_PageContent_txtBayiAdres",
        "ctl00_PageContent_txtPaymentLink", "ctl00_PageContent_TxtDealerName", "ctl00_PageContent_txtBayiYetkiliKisi",
        "ctl00_PageContent_txtBayiWebAdres", "ctl00_PageContent_txtEInvoiceNo",
    };
    private static readonly IReadOnlySet<string> BitDropdownIds = new HashSet<string>(StringComparer.Ordinal)
    {
        "ctl00_PageContent_ddlCurrencyType", "ctl00_PageContent_ddlExpiryDate",
        "ctl00_PageContent_ddlintegrated", "ctl00_PageContent_ddlBonusShow",
    };
    private static readonly IReadOnlySet<string> BooleanIntegerIds = new HashSet<string>(StringComparer.Ordinal)
    {
        "ctl00_PageContent_chkDoNotSendMailToCustomer",
    };
    private static readonly IReadOnlyDictionary<string, (string Column, string SqlType, bool Bitmask)> CatalogWriteAllowlist =
        new Dictionary<string, (string, string, bool)>(StringComparer.Ordinal)
        {
            [SiteStatusId] = ("SiteStatus", "int", false),
            [ParameterOrderId] = ("ParameterOrder", "bigint", true),
            ["ctl00_PageContent_ddlCommonSystem"] = ("ProductCompany", "tinyint", false),
            [ScheduleId] = ("Schedule", "tinyint", false),
            [LayoutId] = ("Layout", "int", false),
            [MenuVisibilityId] = ("DontShowMenuAtDealers", "tinyint", false),
            [DeliveryDateId] = ("DeliveryDateShow", "bit", false),
            ["ctl00_PageContent_ddParameterThema"] = ("ParameterTheme", "int", false),
            ["ctl00_PageContent_ddlProductTableStockType"] = ("ProductTableStockType", "int", false),
            ["ctl00_PageContent_ddlParameterDesign"] = ("ParameterDesign", "bigint", true),
            ["ctl00_PageContent_ddlParameterStock"] = ("ParameterStock", "int", true),
            ["ctl00_PageContent_ddlProductDetailParameter"] = ("ParameterProductDetail", "bigint", true),
            ["ctl00_PageContent_ddlProductDetailParameter2"] = ("ParameterProductDetail2", "bigint", true),
            ["ctl00_PageContent_ddlParameterProductDetailNotLogin"] = ("ParameterProductDetailNotLogin", "bigint", true),
            ["ctl00_PageContent_ddlExportProductList"] = ("ExportProductList", "smallint", true),
            [DealerSettingValueCodec.ParameterPaymentParameterId] = ("ParameterPayment", "smallint", true),
            ["ctl00_PageContent_ddlParameterModules"] = ("ParameterModules", "int", true),
            ["ctl00_PageContent_ddlParameterAccount"] = ("ParameterAccount", "int", true),
            ["ctl00_PageContent_ddMainProducts"] = ("MainProducts", "int", true),
            ["ctl00_PageContent_ddlParameterMenu"] = ("ParameterMenu", "bigint", true),
            ["ctl00_PageContent_ddlParameterDynamicSearch"] = ("ParameterDynamicSearch", "tinyint", true),
            ["ctl00_PageContent_ddlCategoryMenu"] = ("CategoryMenuVisible", "int", false),
            ["ctl00_PageContent_ddlFilterSort"] = ("SortFilterShow", "int", false),
            ["ctl00_PageContent_ddlProductSort"] = ("ProductSort", "int", false),
            ["ctl00_PageContent_ddlCariLimitTracing"] = ("LimitTracing", "int", false),
            ["ctl00_PageContent_ddlCariLimitTracingInfo"] = ("LimitTracingInfo", "int", false),
            ["ctl00_PageContent_ddlDontShowPrice"] = ("DoNotShowPrice", "int", false),
            ["ctl00_PageContent_ddlTaxInRatio"] = ("TaxInRatio", "int", false),
            ["ctl00_PageContent_ddlCartProductVariationCodeShow"] = ("CartProductVariationCodeShow", "int", false),
            ["ctl00_PageContent_ddlHomeStyle"] = ("HomeStyle", "int", false),
            ["ctl00_PageContent_ddlSearchMarka"] = ("SearchShowBrand", "int", false),
            ["ctl00_PageContent_ddlSearchDetail"] = ("SearchShowDetail", "int", false),
            ["ctl00_PageContent_ddlRowDescriptionShow"] = ("RowDescriptionShow", "int", false),
            ["ctl00_PageContent_ddlAllProductsShow"] = ("AllProductsShow", "int", false),
            ["ctl00_PageContent_DDLDeliveryAddress"] = ("DeliveryAddress", "int", false),
            ["ctl00_PageContent_ddlSiteMap"] = ("SiteMap", "int", false),
            ["ctl00_PageContent_ddSubDealerShow"] = ("SubDealerShow", "int", false),
            ["ctl00_PageContent_chkCustomerCodeForLogin"] = ("UseCustomerCodeForLogin", "bit", false),
            ["ctl00_PageContent_chkLoginWithExpiry"] = ("LoginWithExpiry", "bit", false),
            ["ctl00_PageContent_chkDontUseLcnAutoComplate"] = ("DontUseLcnAutoComplate", "bit", false),
            ["ctl00_PageContent_chkAddCartAction"] = ("AddCartAction", "bit", false),
            ["ctl00_PageContent_chkBillingAdress"] = ("BillingAdressControl", "bit", false),
            ["ctl00_PageContent_txtMinCartCount"] = ("MinCartCount", "int", false),
            ["ctl00_PageContent_txtMinCartTotal"] = ("MinCartTotal", "decimal", false),
            ["ctl00_PageContent_txtMaximumOrderQuantity"] = ("MaximumOrderQuantity", "int", false),
            ["ctl00_PageContent_txtProductImageAspectRatio"] = ("ProductImageAspectRatio", "nvarchar", false),
            ["ctl00_PageContent_txtLayoutColor"] = ("LayoutColor", "nvarchar", false),
            ["ctl00_PageContent_txtLayoutColor2"] = ("LayoutColor2", "nvarchar", false),
            ["ctl00_PageContent_txtGoogleAnalysticScript"] = ("GoogleAnalysticScript", "nvarchar", false),
            ["ctl00_PageContent_txtGoogleAnalysticProfileID"] = ("GoogleAnalysticProfileID", "nvarchar", false),
            ["ctl00_PageContent_txtGoogleAdWordsReMarketingCode"] = ("GoogleAdWordsReMarketingCode", "nvarchar", false),
            ["ctl00_PageContent_ddlHasStock"] = ("HasStockVisible", "int", false),
            ["ctl00_PageContent_ddlPaymentDetailShow"] = ("PaymentDetailShow", "int", false),
            ["ctl00_PageContent_txtMinStockFilter"] = ("MinStockFilter", "nvarchar", false),
            ["ctl00_PageContent_ddlBankTableType"] = ("BankTableType", "int", false),
            ["ctl00_PageContent_ddlCurrencyType"] = ("UseCurrencyType", "bit", false),
            ["ctl00_PageContent_ddlExpiryDate"] = ("ExpiryDate", "bit", false),
            ["ctl00_PageContent_ddlAccountActivityGenralCuurencyTableShow"] = ("AccountActivityGenralCuurencyTableShow", "int", false),
            ["ctl00_PageContent_ddlCommissionType"] = ("CommissionType", "int", false),
            ["ctl00_PageContent_ddlSpecialDiscount"] = ("SpecialDiscount", "int", false),
            ["ctl00_PageContent_txtRegisterFormUrl"] = ("RegisterFormUrl", "nvarchar", false),
            ["ctl00_PageContent_ddlPaymentForAccountActivityShow"] = ("PaymentForAccountActivityShow", "int", false),
            ["ctl00_PageContent_txtBayiTelefon"] = ("DealerPhoneNumber", "nvarchar", false),
            ["ctl00_PageContent_txtBayiAdres"] = ("DealerAdress", "nvarchar", false),
            ["ctl00_PageContent_ddlUseDesktopApi"] = ("UseDesktopApi", "int", false),
            ["ctl00_PageContent_ddlProductCurrency"] = ("CurrencyProduct", "int", false),
            ["ctl00_PageContent_ddlPosCurrency"] = ("CurrencyPos", "int", false),
            ["ctl00_PageContent_chkDoNotSendMailToCustomer"] = ("DoNotSendMailToCustomer", "int", false),
            ["ctl00_PageContent_ddlSendDeliveryAddressIfDifferent"] = ("SendDeliveryAddressIfDifferent", "int", false),
            ["ctl00_PageContent_ddlCurrencyPriceType"] = ("CurrencyPriceType", "int", false),
            ["ctl00_PageContent_txtPaymentLink"] = ("PaymentLink", "nvarchar", false),
            ["ctl00_PageContent_ddlintegrated"] = ("Integrated", "bit", false),
            ["ctl00_PageContent_ddldiscountTypeInvoice"] = ("DiscountTypeInvoice", "int", false),
            ["ctl00_PageContent_ddlBonusShow"] = ("BonusShow", "bit", false),
            ["ctl00_PageContent_TxtDealerName"] = ("DealerName", "nvarchar", false),
            ["ctl00_PageContent_txtBayiYetkiliKisi"] = ("DealerAutHorizePersonel", "nvarchar", false),
            ["ctl00_PageContent_txtBayiWebAdres"] = ("DealerWebAdress", "nvarchar", false),
            ["ctl00_PageContent_txtEInvoiceNo"] = ("EInvoiceNo", "nvarchar", false),
            ["ctl00_PageContent_ddlUseMd5Encryption"] = ("UseMd5Encryption", "nvarchar", false),
            ["ctl00_PageContent_ddlVariationShow"] = ("VariationShow", "nvarchar", false),
            ["ctl00_PageContent_ddIntegrationCurrency"] = ("IntegrationCurrencyID", "int", false),
            ["ctl00_PageContent_ddlParameterLogin"] = ("ParameterLogin", "int", true),
            ["ctl00_PageContent_ddlParameterLoginMenu"] = ("ParameterLoginMenu", "int", true),
            ["ctl00_PageContent_ddlParameterAdmin"] = ("ParameterAdmin", "bigint", true),
            ["ctl00_PageContent_ddlParameterIntegration"] = ("ParameterIntegration", "int", true),
            [DealerSettingValueCodec.UseFilteringParameterId] = ("UseFiltering", "nvarchar", true),
            [DealerSettingValueCodec.DiscountTypeParameterId] = ("DiscountType", "nvarchar", false),
        };

    public async Task<PersistentSettingsSaveResult> SaveAsync(
        PersistentSettingsSaveRequest request,
        bool authenticated,
        CancellationToken cancellationToken)
    {
        if (request is null) throw new SettingsException("ValidationFailed", "A persistent settings request is required.");
        if (request.Changes is { Count: > 0 } changes)
        {
            if (request.SiteStatus is not null || request.ParameterOrder is not null || request.Layout is not null
                || request.DontShowMenuAtDealers is not null || request.DeliveryDateShow is not null)
                throw new SettingsException("ValidationFailed", "Use either catalog changes or typed legacy test fields in a request, not both.");
            return await SaveCatalogChangesAsync(changes, authenticated, cancellationToken);
        }
        if (request.Changes is not null)
            throw new SettingsException("ValidationFailed", "At least one catalog setting change is required.");
        if (hostedMode) HostedWriteGuard.EnsureAllowed(testModeEnabled, authenticated, memberCode, connectionString);
        else RollbackWriteTestGuard.EnsureAllowed(testModeEnabled, authenticated, memberCode, connectionString);
        Validate(request);

        await using var connection = new SqlConnection(connectionString);
        try { await connection.OpenAsync(cancellationToken); }
        catch (SqlException) { throw new SettingsException("DatabaseNotConfigured", "The configured SQL database could not be reached."); }
        await EnsureTargetDatabaseAsync(connection, cancellationToken);
        await EnsureSchemaAsync(connection, cancellationToken);

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var committed = false;
        try
        {
            var before = await ReadTargetAsync(connection, transaction, cancellationToken);
            var expectedOrder = before.ParameterOrder;
            if (request.SiteStatus is { ExpectedValue: null } || request.ParameterOrder is { ExpectedValue: null }
                || request.Layout is { ExpectedValue: null } || request.DontShowMenuAtDealers is { ExpectedValue: null }
                || request.DeliveryDateShow is { ExpectedValue: null })
                throw new SettingsException("ValidationFailed", "Persistent test saves require non-NULL originals so the value can be restored exactly.");

            if (request.SiteStatus is { } site)
                await UpdateSiteStatusAsync(connection, transaction, site, cancellationToken);
            if (request.ParameterOrder is { } order)
            {
                var next = BitmaskEditor.Apply(ParseBigint(before.ParameterOrder), order.Operations, AllowedOptionValues());
                expectedOrder = next.ToString(CultureInfo.InvariantCulture);
                await UpdateOrderAsync(connection, transaction, order.ExpectedValue, expectedOrder, cancellationToken);
            }
            if (request.Layout is { } layout)
                await UpdateIntegerAsync(connection, transaction, "Layout", "int", layout.ExpectedValue, layout.NewValue, cancellationToken);
            if (request.DontShowMenuAtDealers is { } menu)
                await UpdateIntegerAsync(connection, transaction, "DontShowMenuAtDealers", "tinyint", menu.ExpectedValue, menu.NewValue, cancellationToken);
            if (request.DeliveryDateShow is { } deliveryDate)
                await UpdateBooleanAsync(connection, transaction, deliveryDate, cancellationToken);

            var readBack = await ReadTargetAsync(connection, transaction, cancellationToken);
            if (request.SiteStatus is { } requestedSite && readBack.SiteStatus != requestedSite.NewValue)
                throw new SettingsException("ReadbackMismatch", "SiteStatus did not match the requested value.");
            if (request.ParameterOrder is not null && !string.Equals(readBack.ParameterOrder, expectedOrder, StringComparison.Ordinal))
                throw new SettingsException("ReadbackMismatch", "ParameterOrder did not match the computed value.");
            if (request.Layout is { } requestedLayout && readBack.Layout != requestedLayout.NewValue)
                throw new SettingsException("ReadbackMismatch", "Layout did not match the requested value.");
            if (request.DontShowMenuAtDealers is { } requestedMenu && readBack.DontShowMenuAtDealers != requestedMenu.NewValue)
                throw new SettingsException("ReadbackMismatch", "Menu visibility did not match the requested value.");
            if (request.DeliveryDateShow is { } requestedDelivery && readBack.DeliveryDateShow != requestedDelivery.NewValue)
                throw new SettingsException("ReadbackMismatch", "Delivery date setting did not match the requested value.");

            await transaction.CommitAsync(cancellationToken);
            committed = true;
        }
        finally
        {
            if (!committed)
            {
                try { await transaction.RollbackAsync(CancellationToken.None); }
                catch (SqlException) { throw new SettingsException("RollbackVerificationFailed", "The isolated test transaction could not be rolled back safely."); }
            }
        }

        // Verify the committed result using a fresh, read-only query.
        var afterCommit = await ReadTargetAsync(connection, null, cancellationToken);
        if (request.SiteStatus is { } expectedSite && afterCommit.SiteStatus != expectedSite.NewValue)
            throw new SettingsException("ReadbackMismatch", "The committed SiteStatus could not be verified.");
        if (request.ParameterOrder is { } expectedParameter)
        {
            var computed = BitmaskEditor.Apply(ParseBigint(expectedParameter.ExpectedValue), expectedParameter.Operations, AllowedOptionValues())
                .ToString(CultureInfo.InvariantCulture);
            if (!string.Equals(afterCommit.ParameterOrder, computed, StringComparison.Ordinal))
                throw new SettingsException("ReadbackMismatch", "The committed ParameterOrder could not be verified.");
        }
        if (request.Layout is { } layoutAfter && afterCommit.Layout != layoutAfter.NewValue
            || request.DontShowMenuAtDealers is { } menuAfter && afterCommit.DontShowMenuAtDealers != menuAfter.NewValue
            || request.DeliveryDateShow is { } deliveryAfter && afterCommit.DeliveryDateShow != deliveryAfter.NewValue)
            throw new SettingsException("ReadbackMismatch", "One or more committed isolated settings could not be verified.");
        return new PersistentSettingsSaveResult(afterCommit.SiteStatus, afterCommit.ParameterOrder, true,
            afterCommit.Layout, afterCommit.DontShowMenuAtDealers, afterCommit.DeliveryDateShow);
    }

    private async Task<PersistentSettingsSaveResult> SaveCatalogChangesAsync(
        IReadOnlyList<SettingChange> changes,
        bool authenticated,
        CancellationToken cancellationToken)
    {
        if (hostedMode) HostedWriteGuard.EnsureAllowed(testModeEnabled, authenticated, memberCode, connectionString);
        else RollbackWriteTestGuard.EnsureAllowed(testModeEnabled, authenticated, memberCode, connectionString);
        if (changes.Count == 0 || changes.Select(change => change.ParameterId).Distinct(StringComparer.Ordinal).Count() != changes.Count)
            throw new SettingsException("ValidationFailed", "Catalog changes must be non-empty and contain each setting once.");
        var definitions = catalog.AllSettings.ToDictionary(setting => setting.ParameterId, StringComparer.Ordinal);
        var requestedChanges = new List<SettingChange>();
        foreach (var change in changes)
        {
            if (!CatalogWriteAllowlist.TryGetValue(change.ParameterId, out var allowed)
                || !definitions.TryGetValue(change.ParameterId, out var definition)
                || definition.Mapping.TableCandidate != "DealerSettings"
                || !string.Equals(definition.Mapping.ColumnCandidate, allowed.Column, StringComparison.Ordinal)
                || !string.Equals(definition.Mapping.SqlDataType, allowed.SqlType, StringComparison.OrdinalIgnoreCase))
                throw new SettingsException("MappingNotVerified", "A setting is not in the fixed write allowlist or its catalog mapping differs.");
            ValidateCatalogControl(definition, allowed.SqlType, allowed.Bitmask);
            if (SettingUpdateValidator.HasNoRequestedValue(change)) continue;
            ValidateCatalogChange(definition, allowed.SqlType, allowed.Bitmask, definition.Mapping.Nullable == true, change);
            requestedChanges.Add(change);
        }
        changes = requestedChanges;
        if (changes.Count == 0)
            throw new SettingsException("ValidationFailed", "No setting values were supplied; no update was requested.");

        await using var connection = new SqlConnection(connectionString);
        try { await connection.OpenAsync(cancellationToken); }
        catch (SqlException) { throw new SettingsException("DatabaseNotConfigured", "The configured SQL database could not be reached."); }
        await EnsureTargetDatabaseAsync(connection, cancellationToken);
        var columnShapes = await EnsureCatalogSchemaAsync(connection, changes.Select(change => CatalogWriteAllowlist[change.ParameterId]).ToArray(), cancellationToken);
        foreach (var change in changes)
        {
            var field = CatalogWriteAllowlist[change.ParameterId];
            var definition = definitions[change.ParameterId];
            var shape = columnShapes[field.Column];
            ValidateCatalogChange(definition, field.SqlType, field.Bitmask, shape.Nullable, change);
            if (field.SqlType is "nvarchar" or "varchar" or "nchar" or "char"
                && change.Value.ValueKind == JsonValueKind.String
                && shape.Size > 0
                && (change.Value.GetString()?.Length ?? 0) > shape.Size)
                throw new SettingsException("ValidationFailed", "The text value exceeds the verified database column length.");
        }
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var committed = false;
        IReadOnlyDictionary<string, object?> projected = new Dictionary<string, object?>();
        try
        {
            var current = await ReadCatalogValuesAsync(connection, transaction, changes, cancellationToken);
            projected = SettingUpdateValidator.ValidateAndProject(catalog,
                new UpdateRequest(changes), current, allowCandidateMappings: true);
            foreach (var change in changes)
            {
                var (column, sqlType, _) = CatalogWriteAllowlist[change.ParameterId];
                var definition = definitions[change.ParameterId];
                var value = projected[change.ParameterId];
                value = DealerSettingValueCodec.ToDatabaseValue(change.ParameterId, value);
                if (BooleanIntegerIds.Contains(change.ParameterId))
                    value = value is true ? "1" : "0";
                else if (BitDropdownIds.Contains(change.ParameterId))
                    value = string.Equals(Convert.ToString(value, CultureInfo.InvariantCulture), "1", StringComparison.Ordinal);
                if (sqlType == "bit" && !BitDropdownIds.Contains(change.ParameterId) && definition.ControlType == "checkbox" && value is not bool)
                    throw new SettingsException("ValidationFailed", "This checkbox requires a Boolean value.");
                var (precision, scale) = change.ParameterId == "ctl00_PageContent_txtMinCartTotal" ? ((byte)18, (byte)2) : ((byte)0, (byte)0);
                var parameter = SqlValueConverter.CreateParameter("@newValue", sqlType, value, precision, scale,
                    columnShapes.TryGetValue(column, out var shape) ? shape.Size : 0);
                await using var command = new SqlCommand(
                    $"UPDATE dbo.DealerSettings SET [{column}]=@newValue OUTPUT INSERTED.MemberCode WHERE MemberCode=@memberCode AND (([{column}]=@expectedValue) OR ([{column}] IS NULL AND @expectedValue IS NULL));",
                    connection, transaction);
                command.Parameters.Add(parameter);
                var expectedValue = SettingUpdateValidator.ReadExpected(change.ExpectedValue);
                expectedValue = DealerSettingValueCodec.ToDatabaseValue(change.ParameterId, expectedValue);
                if (BooleanIntegerIds.Contains(change.ParameterId))
                    expectedValue = expectedValue switch
                    {
                        bool currentBoolean => currentBoolean ? "1" : "0",
                        string currentInteger when currentInteger is "0" or "1" => currentInteger,
                        _ => null,
                    };
                else if (BitDropdownIds.Contains(change.ParameterId))
                    expectedValue = expectedValue switch
                    {
                        bool currentBit => currentBit,
                        string optionValue when optionValue is "0" or "1" => optionValue == "1",
                        _ => null,
                    };
                command.Parameters.Add(SqlValueConverter.CreateParameter("@expectedValue", sqlType,
                    expectedValue, precision, scale, columnShapes.TryGetValue(column, out shape) ? shape.Size : 0));
                command.Parameters.AddWithValue("@memberCode", memberCode!);
                var updatedMemberCode = await command.ExecuteScalarAsync(cancellationToken);
                if (!string.Equals(Convert.ToString(updatedMemberCode, CultureInfo.InvariantCulture), memberCode!, StringComparison.Ordinal))
                    throw new SettingsException("ConcurrencyConflict", "A setting changed before the save.");
            }

            var inTransaction = await ReadCatalogValuesAsync(connection, transaction, changes, cancellationToken);
            foreach (var change in changes)
            {
                var expectedProjection = projected[change.ParameterId];
                if (BooleanIntegerIds.Contains(change.ParameterId)) expectedProjection = expectedProjection is true ? "1" : "0";
                else if (BitDropdownIds.Contains(change.ParameterId))
                    expectedProjection = string.Equals(Convert.ToString(expectedProjection, CultureInfo.InvariantCulture), "1", StringComparison.Ordinal);
                if (!SettingUpdateValidator.ValuesEqual(inTransaction[change.ParameterId], expectedProjection))
                    throw new SettingsException("ReadbackMismatch", "A saved setting did not match the requested value.");
            }
            await transaction.CommitAsync(cancellationToken);
            committed = true;
        }
        finally
        {
            if (!committed)
            {
                try { await transaction.RollbackAsync(CancellationToken.None); }
                catch (SqlException) { throw new SettingsException("RollbackVerificationFailed", "The isolated test transaction could not be rolled back safely."); }
            }
        }

        var afterCommit = await ReadCatalogValuesAsync(connection, null, changes, cancellationToken);
        foreach (var change in changes)
        {
            var expectedProjection = projected[change.ParameterId];
            if (BooleanIntegerIds.Contains(change.ParameterId)) expectedProjection = expectedProjection is true ? "1" : "0";
            else if (BitDropdownIds.Contains(change.ParameterId))
                expectedProjection = string.Equals(Convert.ToString(expectedProjection, CultureInfo.InvariantCulture), "1", StringComparison.Ordinal);
            if (!SettingUpdateValidator.ValuesEqual(afterCommit[change.ParameterId], expectedProjection))
                throw new SettingsException("ReadbackMismatch", "A committed isolated setting could not be verified.");
        }
        return new PersistentSettingsSaveResult(null, null, true, Values: afterCommit);
    }

    private static void ValidateCatalogControl(ParameterDefinition definition, string sqlType, bool bitmask)
    {
        var sql = sqlType.ToLowerInvariant();
        if (definition.ParameterId == DealerSettingValueCodec.UseFilteringParameterId)
        {
            if (!bitmask || definition.ControlType != "bitmask" || !definition.Multiple || sql != "nvarchar"
                || !definition.Options.Select(option => option.Value).SequenceEqual(["2", "4", "8", "16"], StringComparer.Ordinal))
                throw new SettingsException("MappingNotVerified", "UseFiltering requires the observed decimal-text representation and exact source flag set.");
            return;
        }
        if (definition.ParameterId == DealerSettingValueCodec.DiscountTypeParameterId)
        {
            if (bitmask || definition.ControlType != "select" || definition.Multiple || sql != "nvarchar"
                || !definition.Options.Select(option => option.Value).SequenceEqual(["'A'", "'P'", "'C'"], StringComparer.Ordinal))
                throw new SettingsException("MappingNotVerified", "DiscountType requires the observed quoted source codes and scalar text column.");
            return;
        }
        if (definition.ParameterId == ScheduleId)
        {
            if (definition.ControlType != "bitmask" || !definition.Multiple || sql != "tinyint"
                || definition.Options.Count != 1 || definition.Options[0].Value != "1")
                throw new SettingsException("MappingNotVerified", "Schedule requires the verified single-option tinyint adapter.");
            return;
        }
        if (definition.ParameterId == DealerSettingValueCodec.ParameterPaymentParameterId)
        {
            if (!bitmask || definition.ControlType != "bitmask" || !definition.Multiple || sql != "smallint")
                throw new SettingsException("MappingNotVerified", "ParameterPayment requires its verified 16-bit smallint bitmask adapter.");
            foreach (var option in definition.Options)
            {
                if (!ushort.TryParse(option.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var bit)
                    || bit == 0 || (bit & (bit - 1)) != 0)
                    throw new SettingsException("MappingNotVerified", "Every ParameterPayment source option must be a 16-bit power of two.");
            }
            return;
        }
        if (definition.ParameterId == "ctl00_PageContent_ddlParameterModules")
        {
            if (!bitmask || definition.ControlType != "bitmask" || !definition.Multiple || sql != "int"
                || definition.Options.Count != 1 || definition.Options[0].Value != "1")
                throw new SettingsException("MappingNotVerified", "ParameterModules requires only the discovered option value 1 and the int column.");
            return;
        }
        if (bitmask)
        {
            if (definition.ControlType != "bitmask" || !definition.Multiple || definition.Options.Count < 2
                || !new[] { "tinyint", "smallint", "int", "bigint" }.Contains(sql, StringComparer.Ordinal))
                throw new SettingsException("MappingNotVerified", "The control and integral SQL type do not support the approved bitmask adapter.");
            var max = sql switch { "tinyint" => 255L, "smallint" => short.MaxValue, "int" => int.MaxValue, _ => long.MaxValue };
            foreach (var option in definition.Options)
            {
                if (!long.TryParse(option.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var bit)
                    || bit <= 0 || bit > max || (bit & (bit - 1)) != 0)
                    throw new SettingsException("MappingNotVerified", "Every approved bitmask option must be a positive in-range power of two.");
            }
            return;
        }
        if (definition.ControlType == "checkbox" && sql == "bit") return;
        if (BitDropdownIds.Contains(definition.ParameterId))
        {
            if (definition.ControlType != "select" || sql != "bit"
                || !definition.Options.Select(option => option.Value).OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(["0", "1"], StringComparer.Ordinal))
                throw new SettingsException("MappingNotVerified", "The Boolean dropdown does not match its verified SQL bit representation.");
            return;
        }
        if (BooleanIntegerIds.Contains(definition.ParameterId))
        {
            if (definition.ControlType != "checkbox" || sql != "int")
                throw new SettingsException("MappingNotVerified", "The Boolean checkbox does not match its verified integer representation.");
            return;
        }
        if (TextScalarIds.Contains(definition.ParameterId))
        {
            if (definition.ControlType is not ("text" or "textarea") || definition.Multiple
                || sql is not ("nvarchar" or "varchar" or "nchar" or "char" or "text" or "ntext"))
                throw new SettingsException("MappingNotVerified", "The text control does not match its fixed character-column mapping.");
            return;
        }
        if (NumericTextScalarIds.Contains(definition.ParameterId))
        {
            var expectedType = definition.ParameterId == "ctl00_PageContent_txtMinCartTotal" ? "decimal" : "int";
            if (definition.ControlType != "text" || definition.Multiple
                || !string.Equals(sql, expectedType, StringComparison.OrdinalIgnoreCase))
                throw new SettingsException("MappingNotVerified", "The numeric text control no longer matches its fixed scalar mapping.");
            return;
        }
        if (definition.ControlType != "select" || definition.Options.Count == 0)
            throw new SettingsException("MappingNotVerified", "The allowlisted scalar control must be a single-select with source options.");
        foreach (var option in definition.Options)
        {
            try { _ = SqlValueConverter.CreateParameter("@option", sql, option.Value); }
            catch (SettingsException) { throw new SettingsException("MappingNotVerified", "A source option cannot be represented by the mapped SQL type."); }
        }
    }

    public static void ValidateCatalogChange(ParameterDefinition definition, string sqlType, bool bitmask, bool columnNullable, SettingChange change)
    {
        var expected = SettingUpdateValidator.ReadExpected(change.ExpectedValue);
        if (definition.ParameterId == ScheduleId)
        {
            if (expected is null && (definition.Mapping.Nullable != true || !columnNullable))
                throw new SettingsException("ValidationFailed", "A NULL Schedule baseline requires a nullable SQL column.");
            if (expected is not null)
            {
                var expectedText = Convert.ToString(expected, CultureInfo.InvariantCulture);
                if (expectedText is not ("0" or "1"))
                    throw new SettingsException("ValidationFailed", "Schedule accepts only its consumer-supported scalar values.");
            }
            var explicitNull = change.Value.ValueKind == JsonValueKind.Null
                && (change.BitOperations is null || change.BitOperations.Count == 0);
            var optionUpdate = change.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
                && change.BitOperations is { Count: 1 } operations
                && operations[0].OptionValue == "1";
            if (!explicitNull && !optionUpdate)
                throw new SettingsException("ValidationFailed", "Schedule must be set through its single source option or restored to NULL.");
            if (explicitNull && (definition.Mapping.Nullable != true || !columnNullable))
                throw new SettingsException("ValidationFailed", "Schedule NULL is allowed only for a nullable SQL column.");
            if (change.BitOperations is { Count: > 1 } || change.BitOperations is { Count: 1 } bits && bits[0].OptionValue != "1")
                throw new SettingsException("ValidationFailed", "Schedule has only one supported option.");
            return;
        }
        if (expected is null && (definition.Mapping.Nullable != true || !columnNullable))
            throw new SettingsException("ValidationFailed", "A NULL expected value requires a nullable mapped database column.");
        if (expected is not null)
        {
            try
            {
                var sqlExpected = definition.ParameterId == DealerSettingValueCodec.ParameterPaymentParameterId
                    ? DealerSettingValueCodec.ToDatabaseValue(definition.ParameterId, expected)
                    : expected;
                _ = SqlValueConverter.CreateParameter("@expected", sqlType, sqlExpected);
            }
            catch (SettingsException) { throw new SettingsException("ValidationFailed", "The expected value is outside the mapped SQL type range."); }
        }
        if (bitmask)
        {
            if (change.Value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
                || change.BitOperations is null || change.BitOperations.Count == 0)
                throw new SettingsException("ValidationFailed", "Multi-select settings require listed option operations.");
            _ = BitmaskEditor.Apply(0, change.BitOperations, definition.Options.Select(option => option.Value).ToHashSet(StringComparer.Ordinal));
            if (expected is not null && !long.TryParse(Convert.ToString(expected, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                throw new SettingsException("ValidationFailed", "The multi-select baseline must be a decimal integer string.");
            return;
        }
        if (change.BitOperations is { Count: > 0 } || change.Value.ValueKind == JsonValueKind.Undefined)
            throw new SettingsException("ValidationFailed", "Scalar settings require one typed value and no bit operations.");
        var json = change.Value;
        if (json.ValueKind == JsonValueKind.Null)
        {
            if (definition.Mapping.Nullable != true || !columnNullable)
                throw new SettingsException("ValidationFailed", "NULL is allowed only for a nullable mapped SQL column.");
            return;
        }
        if (definition.ControlType == "checkbox")
        {
            if (json.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new SettingsException("ValidationFailed", "Checkbox values must be Boolean.");
            return;
        }
        if (BooleanIntegerIds.Contains(definition.ParameterId))
        {
            if (definition.ControlType != "checkbox" || sqlType != "int" || json.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new SettingsException("ValidationFailed", "This integer-backed checkbox requires a Boolean value.");
            return;
        }
        if (BitDropdownIds.Contains(definition.ParameterId))
        {
            var bitValue = json.ValueKind == JsonValueKind.String ? json.GetString() : null;
            if (definition.ControlType != "select" || sqlType != "bit" || bitValue is not ("0" or "1")
                || !definition.Options.Any(option => option.Value == bitValue))
                throw new SettingsException("ValidationFailed", "The Boolean dropdown value must be one of its source 0/1 options.");
            return;
        }
        if (TextScalarIds.Contains(definition.ParameterId))
        {
            if (definition.ControlType is not ("text" or "textarea") || json.ValueKind != JsonValueKind.String)
                throw new SettingsException("ValidationFailed", "Text settings require a string value.");
            return;
        }
        if (json.ValueKind != JsonValueKind.String)
            throw new SettingsException("ValidationFailed", "Select values must retain their source option strings.");
        var value = json.GetString() ?? string.Empty;
        if (NumericTextScalarIds.Contains(definition.ParameterId))
        {
            var valid = definition.ParameterId == "ctl00_PageContent_txtMinCartTotal"
                ? decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount)
                    && decimal.Round(amount, 2) == amount && decimal.Abs(amount) < 10000000000000000m
                : int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _);
            if (!valid) throw new SettingsException("ValidationFailed", "The numeric setting value is not valid for its verified SQL type.");
            try { _ = SqlValueConverter.CreateParameter("@newValue", sqlType, value, sqlType == "decimal" ? (byte)18 : (byte)0, sqlType == "decimal" ? (byte)2 : (byte)0); }
            catch (SettingsException) { throw new SettingsException("ValidationFailed", "The numeric setting value is outside the verified SQL type range."); }
            return;
        }
        if (!definition.Options.Any(option => string.Equals(option.Value, value, StringComparison.Ordinal)))
            throw new SettingsException("ValidationFailed", "The selected value is not present in the source control options.");
        try { _ = SqlValueConverter.CreateParameter("@newValue", sqlType, value); }
        catch (SettingsException) { throw new SettingsException("ValidationFailed", "The selected value is outside the mapped SQL type range."); }
    }

    private static async Task<IReadOnlyDictionary<string, (int Size, bool Nullable)>> EnsureCatalogSchemaAsync(SqlConnection connection,
        IReadOnlyList<(string Column, string SqlType, bool Bitmask)> fields, CancellationToken token)
    {
        var distinct = fields.DistinctBy(field => field.Column).ToArray();
        await using var command = new SqlCommand();
        command.Connection = connection;
        var parameters = new List<string>();
        for (var index = 0; index < distinct.Length; index++)
        {
            var parameterName = $"@column{index}";
            parameters.Add(parameterName);
            command.Parameters.AddWithValue(parameterName, distinct[index].Column);
        }
        command.CommandText = $"SELECT c.name,t.name,c.precision,c.scale,c.max_length,c.is_nullable FROM sys.columns c JOIN sys.types t ON t.user_type_id=c.user_type_id WHERE c.object_id=OBJECT_ID(N'dbo.DealerSettings') AND c.name IN ({string.Join(',', parameters)});";
        await using var reader = await command.ExecuteReaderAsync(token);
        var found = new Dictionary<string, (string Type, byte Precision, byte Scale, short MaxLength, bool Nullable)>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync(token)) found[reader.GetString(0)] = (reader.GetString(1), reader.GetByte(2), reader.GetByte(3), reader.GetInt16(4), reader.GetBoolean(5));
        var shapes = new Dictionary<string, (int Size, bool Nullable)>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in distinct)
            if (!found.TryGetValue(field.Column, out var actual) || !string.Equals(actual.Type, field.SqlType, StringComparison.OrdinalIgnoreCase)
                || field.Column == "MinCartTotal" && (actual.Precision != 18 || actual.Scale != 2))
                throw new SettingsException("DatabaseIsolationViolation", "The isolated database schema does not match an allowlisted field.");
            else if (field.SqlType is "nvarchar" or "varchar" or "nchar" or "char")
                shapes[field.Column] = (actual.MaxLength == -1 ? 0 : actual.MaxLength / (field.SqlType is "nvarchar" or "nchar" ? 2 : 1), actual.Nullable);
            else
                shapes[field.Column] = (0, actual.Nullable);
        return shapes;
    }

    private async Task<IReadOnlyDictionary<string, object?>> ReadCatalogValuesAsync(SqlConnection connection,
        SqlTransaction? transaction, IReadOnlyList<SettingChange> changes, CancellationToken token)
    {
        var selected = changes.Select(change => CatalogWriteAllowlist[change.ParameterId]).DistinctBy(field => field.Column).ToArray();
        var columns = string.Join(',', selected.Select(field => $"[{field.Column}]"));
        // Read the target row and its match count together. This preserves the
        // duplicate-member safety check while avoiding a separate SQL round trip.
        await using var command = new SqlCommand($"SELECT {columns}, COUNT_BIG(*) OVER() AS [__rowCount] FROM dbo.DealerSettings WITH (UPDLOCK,HOLDLOCK) WHERE MemberCode=@memberCode;", connection, transaction);
        command.Parameters.AddWithValue("@memberCode", memberCode!);
        await using var reader = await command.ExecuteReaderAsync(token);
        if (!await reader.ReadAsync(token)) throw new SettingsException("MemberCodeMissing", "The approved settings row is unavailable.");
        var total = Convert.ToInt64(reader.GetValue(selected.Length), CultureInfo.InvariantCulture);
        RowSelectionGuard.EnsureSingleTarget(total);
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        for (var index = 0; index < selected.Length; index++)
        {
            var raw = reader.GetValue(index);
            var value = raw is DBNull ? null
                : selected[index].SqlType == "bigint" ? Convert.ToInt64(raw, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)
                : reader.GetFieldType(index) == typeof(bool) ? raw
                : Convert.ToString(raw, CultureInfo.InvariantCulture);
            foreach (var change in changes.Where(change => CatalogWriteAllowlist[change.ParameterId].Column == selected[index].Column))
                values[change.ParameterId] = DealerSettingValueCodec.ToApiValue(change.ParameterId, value);
        }
        return values;
    }

    private void Validate(PersistentSettingsSaveRequest request)
    {
        if (request is null || (request.SiteStatus is null && request.ParameterOrder is null && request.Layout is null
            && request.DontShowMenuAtDealers is null && request.DeliveryDateShow is null))
            throw new SettingsException("ValidationFailed", "At least one allowlisted test setting change is required.");
        if (request.SiteStatus is { ExpectedValue: null } || request.ParameterOrder is { ExpectedValue: null }
            || request.Layout is { ExpectedValue: null } || request.DontShowMenuAtDealers is { ExpectedValue: null }
            || request.DeliveryDateShow is { ExpectedValue: null })
            throw new SettingsException("ValidationFailed", "Persistent test saves require a non-NULL original value so it can be restored exactly.");
        if (request.SiteStatus is { NewValue: not (0 or 1) })
            throw new SettingsException("ValidationFailed", "SiteStatus must be Active (1) or Inactive (0).");
        var site = catalog.AllSettings.SingleOrDefault(x => x.ParameterId == SiteStatusId);
        var order = catalog.AllSettings.SingleOrDefault(x => x.ParameterId == ParameterOrderId);
        var layout = catalog.AllSettings.SingleOrDefault(x => x.ParameterId == LayoutId);
        var menu = catalog.AllSettings.SingleOrDefault(x => x.ParameterId == MenuVisibilityId);
        var deliveryDate = catalog.AllSettings.SingleOrDefault(x => x.ParameterId == DeliveryDateId);
        if (site?.Mapping.TableCandidate != "DealerSettings" || site.Mapping.ColumnCandidate != "SiteStatus"
            || order?.Mapping.TableCandidate != "DealerSettings" || order.Mapping.ColumnCandidate != "ParameterOrder"
            || layout?.Mapping.TableCandidate != "DealerSettings" || layout.Mapping.ColumnCandidate != "Layout"
            || menu?.Mapping.TableCandidate != "DealerSettings" || menu.Mapping.ColumnCandidate != "DontShowMenuAtDealers"
            || deliveryDate?.Mapping.TableCandidate != "DealerSettings" || deliveryDate.Mapping.ColumnCandidate != "DeliveryDateShow"
            || !AllowedColumns.SequenceEqual(["SiteStatus", "ParameterOrder", "Layout", "DontShowMenuAtDealers", "DeliveryDateShow"], StringComparer.Ordinal))
            throw new SettingsException("MappingNotVerified", "The fixed test allowlist no longer matches the approved catalog.");
        if (request.SiteStatus is not null
            && (!site.Options.Any(x => x.Value == "1" && x.Label == "Active")
                || !site.Options.Any(x => x.Value == "0" && x.Label == "Inactive")))
            throw new SettingsException("MappingNotVerified", "SiteStatus options do not match the approved mapping.");
        if (request.ParameterOrder is { } parameterOrder)
        {
            if (parameterOrder.Operations is null || parameterOrder.Operations.Count == 0)
                throw new SettingsException("ValidationFailed", "ParameterOrder requires at least one listed option operation.");
            _ = ParseBigint(parameterOrder.ExpectedValue);
            _ = BitmaskEditor.Apply(0, parameterOrder.Operations, AllowedOptionValues());
        }
        if (request.Layout is { } layoutChange)
            ValidateListedScalar(layout!, "Layout", "int", layoutChange.NewValue);
        if (request.DontShowMenuAtDealers is { } menuChange)
            ValidateListedScalar(menu!, "Don't Show Menu At Dealers", "tinyint", menuChange.NewValue);
        if (request.DeliveryDateShow is not null
            && (deliveryDate!.ControlType != "checkbox" || deliveryDate.Mapping.ColumnCandidate != "DeliveryDateShow"))
            throw new SettingsException("MappingNotVerified", "DeliveryDateShow does not match the approved checkbox mapping.");
    }

    private static void ValidateListedScalar(ParameterDefinition definition, string label, string sqlType, int value)
    {
        if (definition.ControlType != "select" || definition.Mapping.SqlDataType is not null
            && !string.Equals(definition.Mapping.SqlDataType, sqlType, StringComparison.OrdinalIgnoreCase)
            || definition.Options.Count == 0
            || !definition.Options.Any(option => int.TryParse(option.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var optionValue)
                && optionValue == value))
            throw new SettingsException("ValidationFailed", $"{label} must use a value listed by its source control.");
    }

    private IReadOnlySet<string> AllowedOptionValues()
    {
        var definition = catalog.AllSettings.Single(x => x.ParameterId == ParameterOrderId);
        var values = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in definition.Options)
        {
            if (!long.TryParse(option.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
                || value <= 0 || (value & (value - 1)) != 0)
                throw new SettingsException("MappingNotVerified", "The ParameterOrder catalog contains an unsupported bit option.");
            values.Add(option.Value);
        }
        return values;
    }

    private static long? ParseBigint(string? value)
    {
        if (value is null) return null;
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)) return parsed;
        throw new SettingsException("ValidationFailed", "ParameterOrder must be a SQL bigint decimal string.");
    }

    private async Task EnsureTargetDatabaseAsync(SqlConnection connection, CancellationToken token)
    {
        await using var command = new SqlCommand("SELECT DB_NAME();", connection);
        var database = Convert.ToString(await command.ExecuteScalarAsync(token), CultureInfo.InvariantCulture);
        if (!string.Equals(database, (hostedMode ? HostedWriteGuard.Database : RollbackWriteTestGuard.TargetDatabase), StringComparison.OrdinalIgnoreCase))
            throw new SettingsException("DatabaseIsolationViolation", "The connected database does not match the configured write target.");
    }

    private static async Task EnsureSchemaAsync(SqlConnection connection, CancellationToken token)
    {
        const string sql = "SELECT c.name,t.name FROM sys.columns c JOIN sys.types t ON t.user_type_id=c.user_type_id WHERE c.object_id=OBJECT_ID(N'dbo.DealerSettings') AND c.name IN (N'SiteStatus',N'ParameterOrder',N'Layout',N'DontShowMenuAtDealers',N'DeliveryDateShow');";
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(token);
        var found = new Dictionary<string, string>(StringComparer.Ordinal);
        while (await reader.ReadAsync(token)) found[reader.GetString(0)] = reader.GetString(1);
        if (!found.TryGetValue("SiteStatus", out var siteType) || siteType != "int"
            || !found.TryGetValue("ParameterOrder", out var orderType) || orderType != "bigint"
            || !found.TryGetValue("Layout", out var layoutType) || layoutType != "int"
            || !found.TryGetValue("DontShowMenuAtDealers", out var menuType) || menuType != "tinyint"
            || !found.TryGetValue("DeliveryDateShow", out var deliveryDateType) || deliveryDateType != "bit")
            throw new SettingsException("DatabaseIsolationViolation", "The isolated test database schema does not match the approved fields.");
    }

    private async Task<Snapshot> ReadTargetAsync(SqlConnection connection, SqlTransaction? transaction, CancellationToken token)
    {
        await using var count = new SqlCommand("SELECT COUNT_BIG(*) FROM dbo.DealerSettings WITH (UPDLOCK,HOLDLOCK) WHERE MemberCode=@memberCode;", connection, transaction);
        count.Parameters.AddWithValue("@memberCode", memberCode!);
        var total = Convert.ToInt64(await count.ExecuteScalarAsync(token), CultureInfo.InvariantCulture);
        RowSelectionGuard.EnsureSingleTarget(total);
        await using var command = new SqlCommand("SELECT [SiteStatus],[ParameterOrder],[Layout],[DontShowMenuAtDealers],[DeliveryDateShow] FROM dbo.DealerSettings WITH (UPDLOCK,HOLDLOCK) WHERE MemberCode=@memberCode;", connection, transaction);
        command.Parameters.AddWithValue("@memberCode", memberCode!);
        await using var reader = await command.ExecuteReaderAsync(token);
        if (!await reader.ReadAsync(token)) throw new SettingsException("MemberCodeMissing", "The approved settings row is unavailable.");
        return new Snapshot(reader.IsDBNull(0) ? null : reader.GetInt32(0),
            reader.IsDBNull(1) ? null : reader.GetInt64(1).ToString(CultureInfo.InvariantCulture),
            reader.IsDBNull(2) ? null : reader.GetInt32(2),
            reader.IsDBNull(3) ? null : Convert.ToInt32(reader.GetValue(3), CultureInfo.InvariantCulture),
            reader.IsDBNull(4) ? null : reader.GetBoolean(4));
    }

    private async Task UpdateSiteStatusAsync(SqlConnection connection, SqlTransaction transaction, PersistentSiteStatusChange change, CancellationToken token)
    {
        const string sql = "UPDATE dbo.DealerSettings SET [SiteStatus]=@newValue WHERE MemberCode=@memberCode AND (([SiteStatus]=@expectedValue) OR ([SiteStatus] IS NULL AND @expectedValue IS NULL));";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add(SqlValueConverter.CreateParameter("@newValue", "int", change.NewValue));
        command.Parameters.Add(SqlValueConverter.CreateParameter("@expectedValue", "int", change.ExpectedValue));
        command.Parameters.AddWithValue("@memberCode", memberCode!);
        if (await command.ExecuteNonQueryAsync(token) != 1) throw new SettingsException("ConcurrencyConflict", "SiteStatus changed before the save.");
    }

    private async Task UpdateOrderAsync(SqlConnection connection, SqlTransaction transaction, string? expectedValue, string newValue, CancellationToken token)
    {
        const string sql = "UPDATE dbo.DealerSettings SET [ParameterOrder]=@newValue WHERE MemberCode=@memberCode AND (([ParameterOrder]=@expectedValue) OR ([ParameterOrder] IS NULL AND @expectedValue IS NULL));";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add(SqlValueConverter.CreateParameter("@newValue", "bigint", newValue));
        command.Parameters.Add(SqlValueConverter.CreateParameter("@expectedValue", "bigint", expectedValue));
        command.Parameters.AddWithValue("@memberCode", memberCode!);
        if (await command.ExecuteNonQueryAsync(token) != 1) throw new SettingsException("ConcurrencyConflict", "ParameterOrder changed before the save.");
    }

    private async Task UpdateIntegerAsync(SqlConnection connection, SqlTransaction transaction, string column, string sqlType, int? expectedValue, int newValue, CancellationToken token)
    {
        var sql = $"UPDATE dbo.DealerSettings SET [{column}]=@newValue WHERE MemberCode=@memberCode AND (([{column}]=@expectedValue) OR ([{column}] IS NULL AND @expectedValue IS NULL));";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add(SqlValueConverter.CreateParameter("@newValue", sqlType, newValue));
        command.Parameters.Add(SqlValueConverter.CreateParameter("@expectedValue", sqlType, expectedValue));
        command.Parameters.AddWithValue("@memberCode", memberCode!);
        if (await command.ExecuteNonQueryAsync(token) != 1) throw new SettingsException("ConcurrencyConflict", $"{column} changed before the save.");
    }

    private async Task UpdateBooleanAsync(SqlConnection connection, SqlTransaction transaction, PersistentBooleanChange change, CancellationToken token)
    {
        const string sql = "UPDATE dbo.DealerSettings SET [DeliveryDateShow]=@newValue WHERE MemberCode=@memberCode AND (([DeliveryDateShow]=@expectedValue) OR ([DeliveryDateShow] IS NULL AND @expectedValue IS NULL));";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add(SqlValueConverter.CreateParameter("@newValue", "bit", change.NewValue));
        command.Parameters.Add(SqlValueConverter.CreateParameter("@expectedValue", "bit", change.ExpectedValue));
        command.Parameters.AddWithValue("@memberCode", memberCode!);
        if (await command.ExecuteNonQueryAsync(token) != 1) throw new SettingsException("ConcurrencyConflict", "DeliveryDateShow changed before the save.");
    }

    private sealed record Snapshot(int? SiteStatus, string? ParameterOrder, int? Layout, int? DontShowMenuAtDealers, bool? DeliveryDateShow);
}
