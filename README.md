# Share

Small showcase projects divided up by folder

## LambdaAnnotations

This is a test of the Lambda Annotations framework and some of its capabilities.


EEF - INPROGRESS [F#873245] NOTES
- We are able to flip this generated API type from HttpApi to Api (which is what our stuff uses)
- NOTED LIMITATION - I don't believe this works cross-account, as it auto-generates in the account it publishes your lambda to - that would be a limitation for stuff like our EIL gateway
  - We can drop the RestApi annotation and just have the lambda generate, if we want.
- There's a stage on our deployments named "live" - if we were to make this multi-environment in a single AWS account, we would change the name of that stage is how that would work
- We can add AutoPublishAlias and the gateway does flip to utilize the live alias
- We can use this on any lambda built through `sam build`/`sam deploy` - we can also continue building it as-is by adjusting the `template` values in `aws-lambda-tools-defaults.json`
- Only one lambda annotations set (project) can be generated per template - if you point two projects to the same template they'll overwrite each other