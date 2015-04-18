select t.id,f.*  from trip t 
INNER JOIN frequency f ON t.id = f.trip_id
INNER JOIN calendar c ON t.service_id = c.service_id
WHERE c.wednesday=1
ORDER BY CAST(f.headway_secs as integer)
