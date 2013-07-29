@echo off
bundle exec thin start -R config.ru -p 8132 -V %*
