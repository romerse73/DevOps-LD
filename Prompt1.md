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
Add retry and parallel cloning of flags
create a json file with the payload use to generate the flag


parameters as strings 
SourceProject(String no spaces),  DestinationProject(String no spaces), FlagKeys(this is an array of strings, the names are only charaters,numbers, '-' , no spaces separator ':')

process the array of Flagkeys from the parameters
then for each item 

read the response to get the flag project  from a api call to 
launchdarkly api  https://app.launchdarkly.com/api/v2/flags/{Sourceproject}/Flagkey
header Authorization  TBD
header content type  application/json

load the response into a json object to remove 
set value for property 'IncludeSnippet' to false 
SET THE AN ADDITIONAL TAG VALUE "CLONNED"

USE THE modified json as string from the object
the operation to create flags would be 
POST. https://app.launchdarkly.com/api/v2/flags/{Destinationproject}/Flagkey   
Header authorizarion
header content type  application/json

Next request
create a github workflow to call the utility that was just created 
and call it


 write the instructions on folder structure to load this in Github for the github workflow to work properly into an MD file