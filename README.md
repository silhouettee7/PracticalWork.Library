# Общие сведения
### Наименование сервиса
PracticalWork.Library

### Назначение
Разработка системы управления библиотекой.

### Интеграции
1. База данных - PostgreSQL
2. Распределенный кэш - Redis
3. Хранение файлов - MinIO
4. Почтовый сервер - smtp4dev
5. Очередь сообщений - RabbitMQ

### Инструменты разработки
1. Rider, Visual Studio 2022 или VS Code
2. PostgreSQL pgAdmin, DBeaver, DataGrip
3. Docker

# Развертывание и конфигурирование сервиса
### Развертывание
- Развертывание интеграций: docker-compose.yaml
- Все настройки в appsettings.json
- Запуск самих приложений в IDE
- PracticalWork.Library.BackgroundTasks - приложение, выполняющее фоновые задачи
- PracticalWork.Library.Web - Запуск основного приложения библиотеки
- PracticalWork.Report.Web - Запуск второго приложения, работающего с бизнес-событиями и отчетами (обрабатываются из очереди) 