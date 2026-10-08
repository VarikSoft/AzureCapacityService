# CSCP Domain One-Pager

## Purpose

CSCP (Capacity, Supply Chain, and Provisioning) is focused on making sure cloud resources can be provided when and where they are needed.

For this project, the main domain question is:

```text
Can this Azure subscription use more resources in the selected region?
```

The Azure Subscription Quota Service answers this by retrieving quota limits, current usage, and available quota.

## Key Concepts

### Subscription

An Azure subscription is a logical boundary for resources, billing, access control, and quotas.

### Region

Many quotas are regional. The same subscription can have different limits and usage in different Azure regions.

### Quota

Quota defines how much of a resource a subscription is allowed to consume.

Example:

```text
Limit: 20 vCPUs
Usage: 8 vCPUs
Available: 12 vCPUs
```

Available quota is calculated as:

```text
Available = Limit - Usage
```

### Capacity

Quota and capacity are not the same.

```text
Quota = how much the subscription is allowed to use
Capacity = whether Azure can physically provide the resource
```

A subscription can have enough quota but still fail to provision a resource if the required VM SKU is not currently available in that region or zone.

## Typical Flow

<p align="left">
  <img src="https://i.imgur.com/xQNoRDY.png" Heigth="300">
</p>

## How This Service Fits

The current service is a read-only API that uses Azure SDK and Azure Quota API.

It provides:

- quota limit;
- current usage;
- available quota;
- quota name and unit;
- all quotas for a region;
- one specific quota by name.

The service currently works with `Microsoft.Compute`.

It does not provision resources or request quota increases. Its purpose is to expose quota information that can be used by provisioning, monitoring, or capacity-management workflows.

## Future Extensions

Possible future improvements include:

- quota increase requests;
- additional Azure resource providers;
- alerts when quota usage is close to the limit;
- caching and periodic refresh;
- integration with provisioning workflows.
