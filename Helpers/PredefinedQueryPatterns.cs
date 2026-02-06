namespace CommonLibrary.Helpers
{
    public class PredefinedQueryPatterns
    {
        public static string DO_OPERATION_QUERY_PATTERN = "select utility.do_operations(@query, @schema, @table, @querytype) as x;";
        public static string DO_SELECT_QUERY_PATTERN = "select utility.do_select('{-QUERY-}'::jsonb, '-SCHEMA-','-TABLE-') as x;";
        public static string GET_VENUE_DATA_REPLACEMENT_JS_QUERY = "select utility.do_operations('{ \"data\": { \"venue_id\": \"\", \"tenant_id\": \"\", \"name\": \"\", \"city\": \"\", \"street\": \"\", \"street_number\": \"\", \"street_addition\": \"\", \"postal_code\": \"\", \"province_code\": \"\", \"province_name\": \"\", \"country_code\": \"\", \"currency_code\": \"\", \"phone\": \"\", \"email\": \"\", \"latitude\": 1.0, \"longitude\": 1.0, \"time_zone\": \"\", \"work_hours\": {}, \"min_lead_mins\": 0, \"max_adv_days\": 0, \"reasons\": {}, \"links\": {}, \"description\": \"\" }, \"order\": [ [\"create_dt\", \"desc\"] ], \"filters\": { \"tenant_id\": \"@tenantID\", \"enabled\": true, \"service_halt\": false, \"is_deleted\": false } }'::jsonb, 'service_portal','venues','select') as x;";
        public static string GET_TENANT_REPLACEMENT_JS_QUERY = "select utility.do_operations('{ \"data\": { \"web_pages\": {}, \"org_code\":\"\", \"library\":{} }, \"filters\": { \"tenant_id\": \"@tenantID\", \"is_deleted\": false, \"status\": {\"values\": [\"ACTIVE\",\"ACTIVE_GDPR\"], \"operator\": \"IN\"} } }'::jsonb, 'help_desk','tenant','select') as x;";
        public static string GET_EXPIRY_TENANT_REPLACEMENT_JS_QUERY = "select utility.do_operations('{ \"data\": { \"domains\": {} }, \"filters\": { \"tenant_id\": \"-TENANT_ID-\", \"is_deleted\": false, \"status\": {\"values\": [\"ACTIVE\",\"ACTIVE_GRPR\"], \"operator\": \"IN\"} } }'::jsonb, 'help_desk','tenant','select') as x;";
        public static string GET_AVAILABILITY_PATTERN = "SELECT utility.get_availability('-VENUE_ID-'::uuid,'-SERVICE-','[-DATE_YYYY_MM_DD-]');";
        public static string BULK_INSERT_OBF_INDEX = "select utility.bulk_insert_obf_index_entries(@payload) as x";
        public static string USERS_TENANT_VIEW_MODEL = "select utility.get_user_view(@tenant_id) as result;";
        public static string VENUES_TENANT_VIEW_MODEL = "select utility.get_venue_view(p_tenant_id := @tenant_id) as result;";
        public static string BOOKING_VENUES_STATISTICS = "select* from utility.get_venue_booking_statistics(@payload) as result";
        public static string BOOKING_LIST = "select* from utility.get_booking_list(@payload) as result";
        public static string BOOKING_DETAIL = "select utility.get_booking_venue_intel(@booking_id) as result;";
        public static string WRITE_ERROR_LOG = "SELECT utility.write_error_log(@payload) as result;";
        public static string RATE_BOOKING = "select utility.rate_booking(@id, @date, @value) as x";
        public static string APPEND_EVENT_BODY = "select utility.append_event_body(@id, @origin, @new_body) as x";
        public static string INSERT_EVENT = "select utility.insert_event(@id, @origin, @message_type, @body, @reference_entity, @reference_id, @tenant_id) as x";
    }

    public class PredefinedQueryPatternsReplacements
    {
        public static string DO_OPERATION_QUERY = "-QUERY";
        public static string DO_OPERATION_SCHEMA = "-SCHEMA-";
        public static string DO_OPERATION_TABLE = "-TABLE";
        public static string DO_OPERATION_QUERYTYPE = "-QUERYTYPE-";
        public static string GET_VENUE_DATA_REPLACEMENT_JS_TENANT = "-TENANT_ID-";
        public static string VENUE_ID = "-VENUE_ID-";
        public static string SERVICE = "-SERVICE-";
        public static string DATE_YYYY_MM_DD = "-DATE_YYYY_MM_DD-";
        //public static string DO_OPERATION_SCHEMA = "-SCHEMA-";
    }

    public enum DoOperationQueryType
    {
        select,
        insert,
        update,
        delete
    };
}
