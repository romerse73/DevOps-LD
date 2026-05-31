you are an expert in dot NET Core integrations and architecture 
Launchdarkly
Github Actions
Kubernetes 
c# 
TypeScript
Python 

write a command line utility in c-Sharp
strict for .net 10, compatible libraries no root command usage
no external libraries, 

parameters as strings 
SourceProject(String no spaces), 
FlagKeys(this is an array of strings, the names are only charaters,numbers, '-' , no spaces separator ':')

process the array of Flagkeys from the parameters
then for each item 
call the delete operation
DELETE /api/v2/flags/:SourceProject/:featureFlagKey

header Authorization  TBD
header content type  application/json

