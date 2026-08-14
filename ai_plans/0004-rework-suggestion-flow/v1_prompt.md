I want to rework the recommendation feature here are some thoughts of mine. Read them and bring them into a useful format to continue work on the suggestion flow

The SuggestionFeature does not work as I want
- GenerateAsync and ReloadAsync should both just return the new List of suggestions instead of a result contract
  due to that remove that completely
- The Suggestion does not need a Title
- The ActivitySignal does not need a Title. Put this into the metadata.
- The ActivitySignal does not need the Link this can also be in the metadata
- The SuggestionModel should contain the metadata of the signal. That is for later comparison usage



The jira signals are not quite usefull since they have no end how to solve that?
- Maybe a lot of signals also when a task got moved, in which status etc.

These signals should the engine take and convert into usefull time ranges.
The engine takes the different noisy signals from all kind of connectors and needs to comapre merge
filter out etc (based on metadata) to get to one merged signal state that could be a suggestion. It 
does it via the metadata and the other fields of the signal.

I want a visialisation of the whole flow with test data from jira teams (calls) github commits etc. and 
which signals they produce and how the engine handles them.
