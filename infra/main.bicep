targetScope = 'resourceGroup'

@description('Azure region for resources.')
param location string = resourceGroup().location

@description('Globally unique Storage account name (3-24 lowercase alphanumeric).')
param storageAccountName string

@description('Static Web App name.')
param staticWebAppName string

@description('Static Web App region (limited set). Free tier.')
@allowed([
  'eastus2'
  'westus2'
  'centralus'
  'eastasia'
  'westeurope'
])
param staticWebAppLocation string = 'eastus2'

module storage './modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    storageAccountName: storageAccountName
  }
}

module swa './modules/swa.bicep' = {
  name: 'swa'
  params: {
    location: staticWebAppLocation
    name: staticWebAppName
  }
}

output storageAccountName string = storage.outputs.accountName
output staticWebAppName string = swa.outputs.name
output staticWebAppDefaultHostname string = swa.outputs.defaultHostname
