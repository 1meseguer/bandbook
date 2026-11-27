#!/bin/bash
# uso: ./ejecutar_todo.sh [usuario]   (pide la contrasena de mysql)
U=${1:-root}
for f in 01_esquema 02_datos 03_consultas_vistas 04_rutinas; do mysql -u "$U" -p < $f.sql || exit 1; done
mysql -u "$U" -p --force < 05_pruebas.sql
