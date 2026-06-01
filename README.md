# Distributed-Transaction-Coordinator-API
If a user places an order, the inventory service reserves stock, and the payment service charges the card. If the payment fails, how do you reliably rollback the inventory without distributed locks?
