This is a very simple test application for sending Google analytics measurements to Google using the `Google Analytics 4` protocol.
Forked from https://github.com/clovett/SimpleGoogleAnalytics

The current Application emulates sessions with associated events.

Important:
=>Please adjust launchSettings.json for you own needs!

Open issues:

1. Sessions don't appear as expected at the GA4 analysis.
I couldn't find helpful documentation on how to track sessions and associate events to sessions with the GA4 Measurement protocol.
I understand that we somehow need to create session_start with parameters ga_session_id and ga_session_number and then send these parameters with the events also.
Info from https://support.google.com/analytics/answer/9234069#session_start :
"A session ID and session number are generated automatically with each session and associated with each event in the session." 
The current application is an attempt only, based on this information.

2. How to send region (country..) information?

See also Todo tags in the source code.



