using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Cloners;

namespace JiangHu.Server;

[Injectable]
public class FixedCustomItemService
{
    private readonly DatabaseService _databaseService;
    private readonly ICloner _cloner;

    public FixedCustomItemService(DatabaseService databaseService, ICloner cloner)
    {
        _databaseService = databaseService;
        _cloner = cloner;
    }

    public CreateItemResult CreateItemFromClone(NewItemFromCloneDetails newItemDetails)
    {
        var result = new CreateItemResult();
        var tables = _databaseService.GetTables();

        var newItemId = newItemDetails.NewId ?? string.Empty;
        if (string.IsNullOrEmpty(newItemId))
        {
            result.Success = false;
            result.Errors.Add("New item ID is missing.");
            return result;
        }

        if (tables.Templates.Items.ContainsKey(newItemId))
        {
            result.Success = false;
            result.Errors.Add($"ItemId already exists: {newItemId}");
            return result;
        }

        var baseTpl = newItemDetails.ItemTplToClone.HasValue ? newItemDetails.ItemTplToClone.Value : default;
        if (!tables.Templates.Items.TryGetValue(baseTpl, out var itemToClone))
        {
            result.Success = false;
            result.Errors.Add($"Base item not found: {baseTpl}");
            return result;
        }

        var itemClone = _cloner.Clone(itemToClone);
        itemClone.Id = newItemId;
        itemClone.Parent = newItemDetails.ParentId;

        UpdateBaseItemPropertiesWithOverrides(newItemDetails.OverrideProperties, itemClone);

        _databaseService.GetItems()[newItemId] = itemClone;

        _databaseService.GetTemplates().Handbook.Items.Add(new HandbookItem
        {
            Id = newItemId,
            ParentId = newItemDetails.HandbookParentId ?? string.Empty,
            Price = newItemDetails.HandbookPriceRoubles
        });

        _databaseService.GetTemplates().Prices[newItemId] = newItemDetails.FleaPriceRoubles ?? 0;

        AddLocalesSafely(newItemDetails.Locales ?? new Dictionary<string, LocaleDetails>(), newItemId);

        result.Success = true;
        result.ItemId = newItemId;
        return result;
    }

    protected void UpdateBaseItemPropertiesWithOverrides(TemplateItemProperties? overrideProperties, TemplateItem itemClone)
    {
        if (overrideProperties is null || itemClone?.Properties is null)
            return;

        var target = itemClone.Properties;
        var targetType = target.GetType();

        foreach (var member in overrideProperties.GetType().GetMembers())
        {
            var value = member.MemberType switch
            {
                MemberTypes.Property => ((PropertyInfo) member).GetValue(overrideProperties),
                MemberTypes.Field => ((FieldInfo) member).GetValue(overrideProperties),
                _ => null,
            };

            if (value is null)
                continue;

            var targetMember = targetType.GetMember(member.Name).FirstOrDefault();
            if (targetMember is null)
                continue;

            switch (targetMember.MemberType)
            {
                case MemberTypes.Property:
                    var prop = (PropertyInfo) targetMember;
                    if (prop.CanWrite)
                        prop.SetValue(target, value);
                    break;

                case MemberTypes.Field:
                    var field = (FieldInfo) targetMember;
                    if (!field.IsInitOnly)
                        field.SetValue(target, value);
                    break;
            }
        }
    }
    private void AddLocalesSafely(Dictionary<string, LocaleDetails> localeDetails, string newItemId)
    {
        var globalLocales = _databaseService.GetLocales().Global;
        if (globalLocales.Count == 0)
        {
            return;
        }

        foreach (var language in globalLocales.Keys.ToList())
        {
            var newLocaleDetails = localeDetails.ContainsKey(language)
                ? localeDetails[language]
                : localeDetails.Values.FirstOrDefault();

            if (newLocaleDetails == null)
            {
                continue;
            }

            var localeData = globalLocales[language].Value;
            if (localeData == null)
            {
                continue;
            }

            var nameKey = $"{newItemId} Name";
            var shortNameKey = $"{newItemId} ShortName";
            var descKey = $"{newItemId} Description";

            localeData[nameKey] = newLocaleDetails.Name ?? string.Empty;
            localeData[shortNameKey] = newLocaleDetails.ShortName ?? string.Empty;
            localeData[descKey] = newLocaleDetails.Description ?? string.Empty;
        }
    }
}
