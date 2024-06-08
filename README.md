# Инструкция по развертыванию S3 MINIO STORAGE
## Шаг 1 — Установка и настройка GO
Заходим на нужный сервер и авторизируемся

Выполняем следующие команды:

Загрузка исполняемого файла Go:
```console
wget -c https://dl.google.com/go/go1.14.2.linux-amd64.tar.gz
```
Распаковка исполняемого файла Go:
```console
tar xvf go1.14.2.linux-amd64.tar.gz
```
Перемещение исполняемого файла Go в /usr/local/ и установка прав:
```console
sudo chown -R root:root ./go
sudo mv go /usr/local
```
Обновление PATH для включения исполняемого файла Go:
```console
sudo echo 'export PATH=$PATH:/usr/local/go/bin' >> /etc/profile
source /etc/profile
```
Проверка установки Go:
```console
go version
```
Удаление загруженного архива с исполняемым файлом Go:
```console
rm go1.14.2.linux-amd64.tar.gz
```
## Шаг 2 — Установка и настройка сервера Mino

Загрузка исполняемого файла MinIO:
```console
cd ~
wget https://dl.min.io/server/minio/release/linux-amd64/minio
```

Создание системного пользователя для MinIO:
```console
sudo useradd --system minio --shell /sbin/nologin
sudo usermod -L minio
sudo chage -E0 minio
```

Перемещение исполняемого файла MinIO в /usr/local/bin/ и установка прав:
```console
sudo mv minio /usr/local/bin
sudo chmod +x /usr/local/bin/minio
sudo chown minio:minio /usr/local/bin/minio
```
Настройка параметров MinIO:
```console
sudo nano /etc/default/minio
```
Заполняем файл следующим образом:
```
MINIO_ACCESS_KEY="minio" #где minio - нужный нам логин
MINIO_VOLUMES="/usr/local/share/minio/"
MINIO_OPTS="-C /etc/minio/certs --address :9000"
MINIO_SECRET_KEY="miniostorage" #где miniostorage - нужный нам пароль
```
Создание необходимых каталогов для MinIO:
```console
sudo mkdir /usr/local/share/minio
sudo mkdir /etc/minio

sudo chown minio:minio /usr/local/share/minio
sudo chown minio:minio /etc/minio
```
Загрузка файла службы systemd для MinIO:
```console
cd ~

wget https://raw.githubusercontent.com/minio/minio-service/master/linux-systemd/minio.service
```
Изменение файла службы systemd:
```console
sed -i 's/User=minio-user/User=minio/g' minio.service
sed -i 's/Group=minio-user/Group=minio/g' minio.service
```
Перемещение файла службы systemd в /etc/systemd/system/:
```console
sudo mv minio.service /etc/systemd/system
```
Перезагрузка systemd и включение/запуск MinIO:
```console
sudo systemctl daemon-reload
sudo systemctl enable minio
sudo systemctl start minio
```
Установка Certbot и получение SSL-сертификатов:
```console
cd ~

sudo apt install software-properties-common
sudo add-apt-repository universe
sudo apt update
sudo apt install certbot
sudo certbot certonly --standalone -d minio-server-test.l4y.ru
```
Копирование SSL-сертификатов в каталог MinIO:
```console
sudo cp /etc/letsencrypt/live/minio-server-test.l4y.ru/privkey.pem /etc/minio/certs/private.key
sudo cp /etc/letsencrypt/live/minio-server-test.l4y.ru/fullchain.pem /etc/minio/certs/public.crt
sudo chown minio:minio /etc/minio/certs/private.key
sudo chown minio:minio /etc/minio/certs/public.crt
```
Перезапуск MinIO с использованием SSL-сертификатов:
```console
sudo systemctl restart minio
```
### ПОСЛЕ МОЖЕМ ПЕРЕЙТИ В ИТЕРФЕЙС S3 ПО ССЫЛКЕ minio-server-test.l4y.ru:9000

## Шаг 3 — Установка API
Клонируем репозиторий в нужную нам папку:
```console
git clone https://github.com/gtssadovod/S3.Minio.StorageApi.git
```
Переходим в папку проекта:
```console
cd S3.Minio.StorageApi/S3.Minio.StorageApi/
```
Меняем логин, пароль, ссылку на S3 который мы указали в пункте 2:

(ФРАГМЕНТ ИЗ п.2)
```
MINIO_ACCESS_KEY="minio" #где minio - нужный нам логин
MINIO_VOLUMES="/usr/local/share/minio/"
MINIO_OPTS="-C /etc/minio/certs --address :9000"
MINIO_SECRET_KEY="miniostorage" #где miniostorage - нужный нам пароль
```
```console
nano Program.cs
```
```

var builder = WebApplication.CreateBuilder(args);

var endpoint = "https://minio-server.sandme.ru:9000"; //МЕНЯЕМ ССЫЛКУ НА НАШУ (minio-server-test.l4y.ru:9000)
var accessKey = "minio"; //МЕНЯЕМ ЛОГИН НА НАШ, КОТОРЫЙ МЫ УКАЗЫВАЛИ В п2
var secretKey = "miniostorage"; //МЕНЯЕМ ПАРОЛЬ НА НАШ, КОТОРЫЙ МЫ УКАЗЫВАЛИ В п2

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.DocumentFilter<SubdomainRouteAttribute>();
});

builder.Services.AddMinio(options =>
{
    options.Endpoint = endpoint;
    options.ConfigureClient(client =>
    {
        client.WithSSL();
    });
});

// Url based configuration
builder.Services.AddMinio(new Uri("s3://minio:miniostorage@minio-server.sandme.ru:9000/region")); //МЕНЯЕМ ССЫЛКУ НА НАШУ ПО ТИПУ: s3://НАШЛОГИН:НАШПАРОЛЬ@minio-server-test.l4y.ru:9000/region
```
Сохраняем и выходим

Запускаем докер-контейнер:
```console
docker-compose upf -d --build
```
Настраиваем nginx, дополняя его:

Если nginx не установлен, установку [см. тут](https://github.com/gtssadovod/l4y.auth)
```console
sudo nano //etc/nginx/sites-available/default
```
```
location /file-storage/ {
    proxy_pass http://127.0.0.1:8087/;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection keep-alive;
    proxy_set_header Host $host;
    proxy_cache_bypass $http_upgrade;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}
```
Полный файл, без учета остальных рабочих сервисов выглядит следующим образом (ПРИМЕР ДЛЯ НАГЛЯДНОСТИ):
```
server {
listen 81;
server_name sandme.ru *.sandme.ru;
location /file-storage/ {
    proxy_pass http://127.0.0.1:8087/;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection keep-alive;
    proxy_set_header Host $host;
    proxy_cache_bypass $http_upgrade;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}
listen 443 ssl; # managed by Certbot
    ssl_certificate /etc/letsencrypt/live/sandme.ru/fullchain.pem; # managed by Certbot
    ssl_certificate_key /etc/letsencrypt/live/sandme.ru/privkey.pem; # managed by Certbot
    include /etc/letsencrypt/options-ssl-nginx.conf; # managed by Certbot
    ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem; # managed by Certbot
    large_client_header_buffers 4 32k;
}
```
Перезапускаем NGINX:
```console
systemctl restart nginx
systemctl reload nginx
```
Переходим по ссылке https://l4y.ru/file-storage/swagger/index.html

