How to run:

docker-compose up -d

run Client.exe

How it works

Client
-> Start http request with idempotency header
-> Select host from service discovery hostlist
-> Send the request to selected service
Service
-> Check if the request is already processed using idempotency header
-> Add idempotency key to cache if it haven't
