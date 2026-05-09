# Trading-bot-on-Bybit

This is a trading bot on Bybit, written in C#. This bot trades futures through the API, it trades according to the impulse breakout scheme, places long and short positions, and closes trades if there is a certain profit or loss. It can be configured in the code. Besides trading, it checks the balance in USDT. To trade and check the balance, you need to insert your own Bybit API keys, otherwise the bot will not work. Find the API variables in the BybitService (Model) file. The bot uses SQLite, and your progress will not be lost.
