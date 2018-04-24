# BAMCIS AreWeUp
An AWS Serverless Application that monitors HTTP, HTTPS, and TCP endpoints for availability.

If an endpoint is found to be down, the Lambda function sends an SNS notification to a specified topic. The function
will run on a schedule the user defines in the CloudFormation template. It can be run in units of seconds, minutes,
hours, or days. For example, you could run it every 30 seconds or you could run it every 7 days.

Optionally, the Lambda function can also deliver metrics to CloudWatch reporting a 1 for up or 0 for down
as well as a latency metric. These metrics are based on the endpoint and have 2 dimensions, 1 is the protocol,
i.e. HTTP, HTTPS, or TCP and the other is a CustomerId. The CustomerId defaults to the AWS account number the
Lambda function is deployed to. However, it can be overriden in the application config file. This gives you
the ability to use the same function to run tests for a multi-tenant environment and separate out reports
from CloudWatch metrics based on the customer id.

The code base has the ability to perform ICMP and UDP health checks as well, but Ping and UDP are not available
in the containers used by AWS Lambda and are thus commented out. You can modify the code to include those checks
and run the application in a Windows or Linux EC2 instance via a scheduled task or cron to perform those checks.

## Usage

The availability tests themselves are all based on a config file that the application reads from S3. The optional
values for **sendToCloudWatch**, **snsTopicArn**, **timeout**, **subject**, and **customerId** all default to
the values configured in the CloudFormation script, which are provided as environment variables to the Lambda function.

If you use test specific SNS topics, make sure you add those topic ARNs to the IAM Policy, `LambdaSNSAreWeUpPolicy`,
attached to the IAM Role, `LambdaAreWeUp`, the Lambda function uses.

Here is an example:

    {
      "http": [
        {
          "path": "www.bamcis.io",
          "sendToCloudWatch": true,
          "timeout": 1000,
          "expectedResponse": 200
        }
      ],
      "https": [
        {
          "path": "https://a.site.com/user_login",
          "method": "POST",
          "contentType": "application/x-www-form-urlencoded",
          "content": "txtId=user&txtPassword=pass&op%5BsignIn%5D=Sign+In",
          "preventAutoRedirect": true,
            "redirectHeadersToValidate": {
              "location": "https://a.site.com/mainmenu"
          },
          "timeout": 500,
          "sendToCloudWatch":  true
        }
      ],
      "tcp": [
        {
          "path": "www.amazon.com",
          "port": 443,
          "sendToCloudWatch": true
        }
      ]
    }

Breaking down the config file, it contains 3 top level keys, **http**, **https**, and **tcp**. These keys 
can be absent or empty arrays if there are no tests for that protocol type. Every test is required to have
a path defined. Other than the path, all the other properties are optional. 

### HTTP

* **path** - The path of the site to check. This can either be a DNS host name, an IP address or a URL.
* **sendToCloudWatch** (optional) - true or false, Sends either a 1 if the site is up or 0 if it is down (or no
datapoint on error). If successful, it will also send a latency metric. The default for this value is false.
* **expectedResponse** (optional) - The http response code expected, defaults to 200.
* **method** (optional) - The HTTP method to use like GET, POST, etc. This defaults to HEAD.
* **content** (optiona): The content to send in the body of the request for POST, PATCH, and PUT requests.
* **contentType** (optional) - The type of the content that is being sent, like application/json, required if
content is specified.
* **preventAutoRedirect** (optional) - Stops the processing of a redirect so the 3XX response can be inspected, 
defaults to false.
* **redirectHeadersToValidate** (optional) - Key value pairs of the header name and expected value that will be validated
on a redirect response. This requires setting preventAutoRedirect to true.
* **cookiesToValidate** (optional) - Tests for the presence of these cookies after the response
* **port** (optional): The port to test on, this defaults to 80.
* **timeout** (optional) - The amount of time in milliseconds to wait until the test times out. Default is 50.
* **customerId** (optional) - A unique Id to associate with the request, this becomes a dimension in the CloudWatch metric. The
default is the current AWS account id.
* **snsTopicArn** (optional) - A specifc ARN to send SNS notifications to, this defaults to the ARN provided to
the lambda function by the CloudFormation template.
* **subject** (optional) - The email subject line to be used with the SNS notification.

### HTTPS

All configuration options for **HTTP** are available for HTTPS. Additionally, the following properties are available:

* **ignoreSSLErrors** (optional) - true or false, Setting this to true will ignore SSL cert erros like being
expired or being signed by an untrusted CA. The error will be logged in CloudWatch logs. The default is false.
* **port** (optional): The port to test on, this defaults to 443.

### TCP

* **path** - The IPv4 address or DNS host name to check.
* **port** - The port to test on.
* **timeout** (optional) - The amount of time in milliseconds to wait until the test times out. Default is 50.
* **sendToCloudWatch** (optional) - true or false, Sends either a 1 if the site is up or 0 if it is down (or no
datapoint on error). If successful, it will also send a latency metric. The default for this value is false.
* **customerId** (optional) - A unique Id to associate with the request, this becomes a dimension in the CloudWatch metric. The
default is the current AWS account id.
* **snsTopicArn** (optional) - A specifc ARN to send SNS notifications to, this defaults to the ARN provided to
the lambda function by the CloudFormation template.
* **subject** (optional) - The email subject line to be used with the SNS notification.

### Use cases

To highlight a use case in the above example for HTTPS. In order to test the functionality of the application,
the website must be tested by logging in to it. The site always issues a redirect on login, both for failed 
and successful attempts. The only way to tell if the login was successful is via the location header that is
returned in the redirect response. Thus, the Lambda function will POST the login form data to the website, prevent
the automatic redirect and inspect the location header in the response to gauge if the availability test was
successful or not.

## Revision History

### 1.0.0
Initial release of the application.
