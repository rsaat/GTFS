select p.poi_name,s.stop_name  FROM poi p 
inner join poi_stop ps ON p.poi_id=ps.poi_id_fk
INNER JOIN stop s ON s.id=ps.stop_id_fk
ORDER BY p.poi_name,s.stop_name
