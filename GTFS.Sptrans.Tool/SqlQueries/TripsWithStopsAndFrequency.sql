SELECT st.trip_id,r.route_short_name,t.trip_headsign,r.route_long_name,s.id,s.stop_name
from  stop_time st 
INNER JOIN trip t ON t.id = st.trip_id 
INNER JOIN route r ON r.id=t.route_id
INNER JOIN frequency f ON f.trip_id = t.id
INNER JOIN stop s ON s.id = st.stop_id 


WHERE st.stop_id IN ('830004111')
and r.route_short_name like '%%%'


ORDER BY  t.id