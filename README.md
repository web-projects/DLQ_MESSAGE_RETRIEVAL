### What is this repository for? ###

* DLQ_MESSAGE_RETRIEVAL
* 1.00.0.(100)
* git remote add origin https://github.com/web-projects/DLQ_MESSAGE_RETRIEVAL.git

# Azure Bus Topics and Subscriptions

We use Topics when there are multiple subscribers that want to consume the same message.

Individual subscribers will be consuming the messages from the individual subscription of the same topic. Subscribers can define which messages they want to receive from a topic using the filter.

When there is an exception generated at the application you can refer to different exception types that will help you to understand cause, and notes suggested action you can take. Let's say if your consumer application has peaked the messages but was not able to call "CompleteAsync()" on that messages due to any reasons (application crash, server restarted, etc.) then once the lock expires (after lock duration) on that message then the message will be again visible for your consumer to be consumed. The brokered message delivery count property will keep on increasing for that message for every failure your client application is not able to consume that message. Once it has reached the MaxDeliveryCount the message will be deleted (if you have disabled dead lettering property of subscription) or moved to dead letter queue.

You can update the MaxDeliveryCount property (default value 10) to your own value according to your requirement and use service bus metrics (Dead -letterd messages) to setup the alter to notify you if there is any message in the dead letter queue.

Azure service bus SDK already has a default retry mechanism and you can refer to this article for more details.

Note: You need to refer to expectation type to understand in which exception the inbuild retry works. If the "inbuild retry doesn't help" then you need to implement your own retry logic to retry that operation. When there is a client issue/error/code issue from client side the inbuild retry doesn't help.


# Dead-Letter Queues 

This sample shows how to move messages to the Dead-letter queue, how to retrieve
messages from it, and resubmit corrected message back into the main queue. 

## What is a Dead-Letter Queue? 

All Service Bus Queues and Subscriptions have a secondary sub-queue, called the
*dead-letter queue* (DLQ). 

This sub-queue does not need to be explicitly created and cannot be deleted or
otherwise managed independent of the main entity. The purpose of the Dead-Letter
Queue (DLQ) is accept and hold messages that cannot be delivered to any receiver
or messages that could not be processed. Read more about Dead-Letter Queues [in
the product documentation.][1]

## Sample Code 

The sample implements two scenarios:

* Send a message and then retrierve and abandon the message until the maximum
delivery count is exhausted and the message is automatically dead-lettered. 

* Send a set of messages, and explicitly dead-letter messages that do not match
a certain criterion and would therefore not be processed correctly. The messages
are then picked up from the dead-letter queue, are automatically corrected, and
resubmitted.  

The sample code is further documented inline in the [Program.cs](Program.cs) C# file.

[1]: https://docs.microsoft.com/azure/service-bus-messaging/service-bus-dead-letter-queues

### GIT NOTES ###

*  AUTO-CONVERTING CRLF line endings into LF
   $ git config --global core.autocrlf true

### GIT REPOSITORY TAGGING ###

* git tag -a GA_RELEASE_1_00_0_00_001 -m "GA_RELEASE_1_00_0_00_001"

### HISTORY ###

* 20221202 - Initial repository.
* 20221212 - Added Dependency Injection to Communication Modules.
* 20221213 - GA_RELEASE_1_00_0_00_001
